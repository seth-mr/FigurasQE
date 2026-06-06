const amqp = require('amqplib');

const RABBITMQ_URL = process.env.RABBITMQ_URL || 'amqp://guest:guest@localhost:5672';
const RABBITMQ_QUEUE = process.env.RABBITMQ_QUEUE || 'fqe.logs';

let connectionPromise = null;
let channelPromise = null;

function resetConnectionState() {
    connectionPromise = null;
    channelPromise = null;
}

async function getChannel() {
    if (!connectionPromise) {
        connectionPromise = amqp.connect(RABBITMQ_URL);

        connectionPromise
            .then((connection) => {
                connection.on('close', resetConnectionState);
                connection.on('error', () => {});
                return connection;
            })
            .catch((error) => {
                resetConnectionState();
                throw error;
            });
    }

    if (!channelPromise) {
        channelPromise = connectionPromise
            .then(async (connection) => {
                const channel = await connection.createChannel();
                await channel.assertQueue(RABBITMQ_QUEUE, { durable: true });
                return channel;
            })
            .catch((error) => {
                channelPromise = null;
                throw error;
            });
    }

    return channelPromise;
}

async function publishGatewayLog(event) {
    const channel = await getChannel();
    const content = Buffer.from(JSON.stringify(event));

    channel.sendToQueue(RABBITMQ_QUEUE, content, {
        contentType: 'application/json',
        persistent: true,
    });
}

module.exports = {
    publishGatewayLog,
};
