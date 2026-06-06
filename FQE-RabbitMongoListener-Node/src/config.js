const dotenv = require("dotenv");

dotenv.config();

function toBoolean(value, defaultValue) {
  if (value === undefined || value === null || value === "") {
    return defaultValue;
  }

  const normalized = String(value).trim().toLowerCase();
  return normalized === "true" || normalized === "1" || normalized === "yes";
}

function toNumber(value, defaultValue) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : defaultValue;
}

const config = {
  rabbitmq: {
    host: process.env.RABBITMQ_HOST || "localhost",
    port: toNumber(process.env.RABBITMQ_PORT, 5672),
    username: process.env.RABBITMQ_USERNAME || "guest",
    password: process.env.RABBITMQ_PASSWORD || "guest",
    vhost: process.env.RABBITMQ_VHOST || "/",
    queue: process.env.RABBITMQ_QUEUE || "fqe.logs",
    prefetch: toNumber(process.env.RABBITMQ_PREFETCH, 20),
    autoDeclareQueue: toBoolean(process.env.RABBITMQ_AUTO_DECLARE, true),
    durableQueue: toBoolean(process.env.RABBITMQ_DURABLE_QUEUE, true)
  },
  mongo: {
    connectionString:
      process.env.MONGO_CONNECTION_STRING ||
      "mongodb://localhost:27017/logsdb",
    database: process.env.MONGO_DATABASE || "logsdb",
    collection: process.env.MONGO_COLLECTION || "events"
  },
  health: {
    host: process.env.HEALTH_HOST || "0.0.0.0",
    port: toNumber(process.env.HEALTH_PORT, 5190)
  },
  reconnectDelayMs: toNumber(process.env.RECONNECT_DELAY_MS, 5000),
  logLevel: process.env.LOG_LEVEL || "info"
};

module.exports = { config };
