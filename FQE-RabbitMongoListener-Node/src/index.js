const http = require("http");
const swaggerApp = require("./swagger");
// Iniciar Swagger UI en Express (puerto 5191 para evitar colisión)
swaggerApp.listen(5191, () => {
  logger.info({ port: 5191 }, "Swagger UI available at /swagger");
});
const amqp = require("amqplib");
const { config } = require("./config");
const { logger } = require("./logger");
const { MongoEventRepository } = require("./mongoEventRepository");

const repository = new MongoEventRepository(config.mongo, logger);

let shouldStop = false;
let activeConnection = null;
let activeChannel = null;
let healthServer = null;

const healthState = {
  startedAt: new Date().toISOString(),
  rabbitmqConnected: false,
  mongoConnected: false,
  lastError: null,
  reconnectDelayMs: config.reconnectDelayMs
};

function updateHealthState(updates) {
  Object.assign(healthState, updates);
}

function buildHealthPayload() {
  const isHealthy = healthState.rabbitmqConnected && healthState.mongoConnected;

  return {
    service: "rabbit-mongo-listener",
    status: isHealthy ? "ok" : "degraded",
    rabbitmq: healthState.rabbitmqConnected ? "ok" : "down",
    mongo: healthState.mongoConnected ? "ok" : "down",
    uptimeSeconds: Math.floor(process.uptime()),
    reconnectDelayMs: healthState.reconnectDelayMs,
    startedAt: healthState.startedAt,
    lastError: healthState.lastError
  };
}

function startHealthServer() {
  healthServer = http.createServer((req, res) => {
    if (req.method !== "GET") {
      res.writeHead(405, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ message: "Method not allowed" }));
      return;
    }

    if (req.url !== "/health") {
      res.writeHead(404, { "Content-Type": "application/json" });
      res.end(JSON.stringify({ message: "Not found" }));
      return;
    }

    const payload = buildHealthPayload();
    const statusCode = payload.status === "ok" ? 200 : 503;

    res.writeHead(statusCode, { "Content-Type": "application/json" });
    res.end(JSON.stringify(payload));
  });

  healthServer.listen(config.health.port, config.health.host, () => {
    logger.info(
      { host: config.health.host, port: config.health.port },
      "Health endpoint listening"
    );
  });
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function consumeLoop() {
  while (!shouldStop) {
    try {
      await repository.connect();
      updateHealthState({ mongoConnected: true, lastError: null });

      const connection = await amqp.connect({
        protocol: "amqp",
        hostname: config.rabbitmq.host,
        port: config.rabbitmq.port,
        username: config.rabbitmq.username,
        password: config.rabbitmq.password,
        vhost: config.rabbitmq.vhost
      });

      const channel = await connection.createChannel();
      activeConnection = connection;
      activeChannel = channel;
  updateHealthState({ rabbitmqConnected: true, lastError: null });

      await channel.prefetch(config.rabbitmq.prefetch);

      if (config.rabbitmq.autoDeclareQueue) {
        await channel.assertQueue(config.rabbitmq.queue, {
          durable: config.rabbitmq.durableQueue,
          exclusive: false,
          autoDelete: false
        });
      }

      logger.info(
        {
          queue: config.rabbitmq.queue,
          host: config.rabbitmq.host,
          port: config.rabbitmq.port
        },
        "Listening for RabbitMQ messages"
      );

      await channel.consume(
        config.rabbitmq.queue,
        async (message) => {
          if (!message) {
            return;
          }

          const rawMessage = message.content.toString("utf-8");
          const metadata = {
            deliveryTag: message.fields.deliveryTag,
            exchange: message.fields.exchange,
            routingKey: message.fields.routingKey,
            redelivered: message.fields.redelivered
          };

          try {
            await repository.saveMessage(rawMessage, metadata);
            channel.ack(message);
            logger.info({ deliveryTag: metadata.deliveryTag }, "Message processed and saved in MongoDB");
          } catch (error) {
            logger.error(
              { err: error, deliveryTag: metadata.deliveryTag },
              "Error processing message. Requeueing message"
            );
            channel.nack(message, false, true);
          }
        },
        { noAck: false }
      );

      await new Promise((resolve, reject) => {
        connection.on("close", () => {
          updateHealthState({ rabbitmqConnected: false, lastError: "RabbitMQ connection closed" });
          if (!shouldStop) {
            reject(new Error("RabbitMQ connection closed"));
          } else {
            resolve();
          }
        });

        connection.on("error", (error) => {
          updateHealthState({ rabbitmqConnected: false, lastError: error.message });
          if (!shouldStop) {
            reject(error);
          }
        });
      });
    } catch (error) {
      if (shouldStop) {
        break;
      }

      updateHealthState({
        rabbitmqConnected: false,
        mongoConnected: false,
        lastError: error.message
      });

      logger.error({ err: error }, "RabbitMQ connection/consumption failed. Retrying");
      await sleep(config.reconnectDelayMs);
    } finally {
      try {
        if (activeChannel) {
          await activeChannel.close();
        }
      } catch {
        // ignore close errors during reconnect
      }

      try {
        if (activeConnection) {
          await activeConnection.close();
        }
      } catch {
        // ignore close errors during reconnect
      }

      activeChannel = null;
      activeConnection = null;
      updateHealthState({ rabbitmqConnected: false, mongoConnected: false });
    }
  }
}

async function shutdown(signal) {
  if (shouldStop) {
    return;
  }

  shouldStop = true;
  logger.info({ signal }, "Shutting down listener");

  try {
    if (activeChannel) {
      await activeChannel.close();
    }
  } catch {
    // ignore shutdown close errors
  }

  try {
    if (activeConnection) {
      await activeConnection.close();
    }
  } catch {
    // ignore shutdown close errors
  }

  try {
    await repository.close();
    updateHealthState({ mongoConnected: false });
  } catch (error) {
    logger.error({ err: error }, "Error closing MongoDB connection");
  }

  try {
    if (healthServer) {
      await new Promise((resolve, reject) => {
        healthServer.close((error) => {
          if (error) {
            reject(error);
            return;
          }

          resolve();
        });
      });
    }
  } catch (error) {
    logger.error({ err: error }, "Error closing health server");
  }

  process.exit(0);
}

process.on("SIGINT", () => {
  shutdown("SIGINT");
});

process.on("SIGTERM", () => {
  shutdown("SIGTERM");
});

startHealthServer();

consumeLoop().catch((error) => {
  logger.fatal({ err: error }, "Fatal error in consume loop");
  process.exit(1);
});
