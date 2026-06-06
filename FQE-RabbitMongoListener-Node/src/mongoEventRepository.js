const { MongoClient } = require("mongodb");

class MongoEventRepository {
  constructor(mongoConfig, logger) {
    this.mongoConfig = mongoConfig;
    this.logger = logger;
    this.client = null;
    this.collection = null;
  }

  async connect() {
    if (this.collection) {
      return;
    }

    this.client = new MongoClient(this.mongoConfig.connectionString);
    await this.client.connect();

    const database = this.client.db(this.mongoConfig.database);
    this.collection = database.collection(this.mongoConfig.collection);

    this.logger.info(
      {
        database: this.mongoConfig.database,
        collection: this.mongoConfig.collection
      },
      "Connected to MongoDB"
    );
  }

  async saveMessage(rawJson, metadata) {
    const now = new Date();
    let messageDocument;

    try {
      messageDocument = JSON.parse(rawJson);
      if (typeof messageDocument !== "object" || messageDocument === null) {
        messageDocument = { rawMessage: rawJson };
      }
    } catch {
      messageDocument = { rawMessage: rawJson };
    }

    if (metadata && Object.keys(metadata).length > 0) {
      messageDocument.rabbitMetadata = metadata;
    }

    if (!Object.prototype.hasOwnProperty.call(messageDocument, "timestamp") || messageDocument.timestamp == null) {
      const receivedAtCandidate = messageDocument.receivedAtUtc;
      const receivedAtDate = receivedAtCandidate ? new Date(receivedAtCandidate) : null;
      const hasValidReceivedAt = receivedAtDate instanceof Date && !Number.isNaN(receivedAtDate.getTime());

      messageDocument.timestamp = hasValidReceivedAt ? receivedAtDate : now;
    }

    messageDocument.receivedAtUtc = now;

    await this.collection.insertOne(messageDocument);
  }

  async close() {
    if (this.client) {
      await this.client.close();
      this.client = null;
      this.collection = null;
    }
  }
}

module.exports = { MongoEventRepository };
