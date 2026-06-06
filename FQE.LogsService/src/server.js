const http = require('http');
const express = require('express');
const { MongoClient, ObjectId } = require('mongodb');
const swaggerUi = require('swagger-ui-express');
const { WebSocketServer, OPEN } = require('ws');
const dotenv = require('dotenv');
const openApiDocument = require('./swagger');

dotenv.config();

const HOST = process.env.HOST || '0.0.0.0';
const PORT = Number.parseInt(process.env.PORT || '5186', 10);
const MONGO_URI = process.env.MONGO_URI || 'mongodb://localhost:27017';
const MONGO_DATABASE = process.env.MONGO_DATABASE || 'logsdb';
const MONGO_COLLECTION = process.env.MONGO_COLLECTION || 'events';
const DEFAULT_LIMIT = Number.parseInt(process.env.DEFAULT_LIMIT || '200', 10);
const MAX_LIMIT = Number.parseInt(process.env.MAX_LIMIT || '1000', 10);
const POLL_INTERVAL_MS = Number.parseInt(process.env.POLL_INTERVAL_MS || '1000', 10);

const app = express();
const server = http.createServer(app);
const wsServer = new WebSocketServer({ noServer: true });

app.get('/openapi.json', (_req, res) => {
  res.json(openApiDocument);
});

app.use('/swagger', swaggerUi.serve, swaggerUi.setup(openApiDocument, {
  explorer: true,
  customSiteTitle: 'FQE Logs Service Swagger'
}));

let mongoClient;
let collection;

function toUtcDate(value) {
  if (!value) {
    return null;
  }

  if (value instanceof Date && Number.isFinite(value.getTime())) {
    return value;
  }

  if (typeof value === 'string') {
    const parsed = new Date(value);
    if (Number.isFinite(parsed.getTime())) {
      return parsed;
    }
  }

  return null;
}

function normalizeLog(document) {
  const objectId = document._id instanceof ObjectId ? document._id : null;

  let timestamp = toUtcDate(document.timestamp);
  if (!timestamp) {
    timestamp = toUtcDate(document.receivedAtUtc);
  }
  if (!timestamp && objectId) {
    timestamp = objectId.getTimestamp();
  }
  if (!timestamp) {
    timestamp = new Date();
  }

  const statusCode = document.statusCode == null ? null : Number.parseInt(document.statusCode, 10);
  const durationMs = document.durationMs == null ? null : Number.parseInt(document.durationMs, 10);
  const entityId = document.entityId == null ? null : Number.parseInt(document.entityId, 10);

  return {
    id: objectId ? objectId.toHexString() : '',
    service: String(document.service || 'unknown'),
    route: document.route ?? null,
    statusCode: Number.isFinite(statusCode) ? statusCode : null,
    durationMs: Number.isFinite(durationMs) ? durationMs : null,
    timestamp: timestamp.toISOString(),
    type: String(document.type || 'event'),
    entityType: document.entityType ?? null,
    entityId: Number.isFinite(entityId) ? entityId : null,
    action: String(document.action || 'unknown')
  };
}

function equalsFilter(value, expected) {
  if (!expected || !expected.trim()) {
    return true;
  }

  return String(value || '').trim().toLowerCase() === expected.trim().toLowerCase();
}

function matchesRouteFilter(routeValue, routeFilter) {
  if (!routeFilter || !routeFilter.trim()) {
    return true;
  }

  return String(routeValue || '').toLowerCase().includes(routeFilter.trim().toLowerCase());
}

function matchesStatusClass(statusCode, statusClass) {
  if (!statusClass || !statusClass.trim()) {
    return true;
  }

  const normalized = statusClass.trim().toLowerCase();
  const code = Number.parseInt(statusCode, 10);
  if (!Number.isFinite(code)) {
    return false;
  }

  if (/^[2-5]xx$/.test(normalized)) {
    return Math.floor(code / 100) === Number.parseInt(normalized[0], 10);
  }

  if (/^[2-5]$/.test(normalized)) {
    return Math.floor(code / 100) === Number.parseInt(normalized, 10);
  }

  if (normalized === 'success' || normalized === 'ok') {
    return code >= 200 && code < 300;
  }
  if (normalized === 'redirect' || normalized === 'redirection') {
    return code >= 300 && code < 400;
  }
  if (normalized === 'client' || normalized === 'client_error') {
    return code >= 400 && code < 500;
  }
  if (normalized === 'server' || normalized === 'server_error' || normalized === 'error') {
    return code >= 500 && code < 600;
  }

  return false;
}

function matchesFilters(payload, filters) {
  return (
    equalsFilter(payload.service, filters.service)
    && equalsFilter(payload.type, filters.type)
    && equalsFilter(payload.entityType, filters.entityType)
    && equalsFilter(payload.action, filters.action)
    && matchesStatusClass(payload.statusCode, filters.statusClass)
    && matchesRouteFilter(payload.route, filters.route)
  );
}

function parseLimit(rawLimit) {
  const parsed = Number.parseInt(rawLimit ?? `${DEFAULT_LIMIT}`, 10);
  if (!Number.isFinite(parsed)) {
    return DEFAULT_LIMIT;
  }

  return Math.min(Math.max(parsed, 1), MAX_LIMIT);
}

function buildMongoQuery(filters) {
  const query = {};

  if (filters.service) {
    query.service = filters.service;
  }
  if (filters.type) {
    query.type = filters.type;
  }
  if (filters.entityType) {
    query.entityType = filters.entityType;
  }
  if (filters.action) {
    query.action = filters.action;
  }
  if (filters.route) {
    query.route = { $regex: filters.route, $options: 'i' };
  }

  return query;
}

async function loadLogs(filters, limit) {
  const mongoQuery = buildMongoQuery(filters);
  const docs = await collection
    .find(mongoQuery)
    .sort({ _id: -1 })
    .limit(limit * 5)
    .toArray();

  return docs
    .map(normalizeLog)
    .filter((payload) => matchesFilters(payload, filters))
    .slice(0, limit);
}

app.get('/health', async (_req, res) => {
  try {
    await mongoClient.db('admin').command({ ping: 1 });
    res.status(200).json({ service: 'logs', status: 'ok', mongo: 'ok' });
  } catch (error) {
    res.status(200).json({
      service: 'logs',
      status: 'degraded',
      mongo: 'error',
      message: error.message
    });
  }
});

app.get('/api/logs', async (req, res) => {
  try {
    const filters = {
      service: req.query.service || null,
      type: req.query.type || null,
      entityType: req.query.entityType || null,
      action: req.query.action || null,
      statusClass: req.query.statusClass || null,
      route: req.query.route || null
    };

    const logs = await loadLogs(filters, parseLimit(req.query.limit));
    res.status(200).json(logs);
  } catch (error) {
    res.status(500).json({ message: error.message || 'Unable to load logs.' });
  }
});

app.get('/api/logs/service/:serviceName', async (req, res) => {
  try {
    const filters = {
      service: req.params.serviceName || null,
      type: req.query.type || null,
      entityType: req.query.entityType || null,
      action: req.query.action || null,
      statusClass: req.query.statusClass || null,
      route: req.query.route || null
    };

    const logs = await loadLogs(filters, parseLimit(req.query.limit));
    res.status(200).json(logs);
  } catch (error) {
    res.status(500).json({ message: error.message || 'Unable to load logs.' });
  }
});

server.on('upgrade', (request, socket, head) => {
  const url = new URL(request.url, 'http://localhost');
  if (url.pathname !== '/ws/logs') {
    return;
  }

  wsServer.handleUpgrade(request, socket, head, (clientSocket) => {
    wsServer.emit('connection', clientSocket, request, url);
  });
});

wsServer.on('connection', (socket, _request, url) => {
  const filters = {
    service: url.searchParams.get('service'),
    type: url.searchParams.get('type'),
    entityType: url.searchParams.get('entityType'),
    action: url.searchParams.get('action'),
    statusClass: url.searchParams.get('statusClass'),
    route: url.searchParams.get('route')
  };

  // token is accepted for compatibility with gateway and admin client websocket flow.
  const token = url.searchParams.get('token');
  void token;

  let closed = false;
  let lastSeenId = null;

  socket.on('close', () => {
    closed = true;
  });

  const pump = async () => {
    while (!closed) {
      try {
        const query = {};
        if (lastSeenId) {
          query._id = { $gt: lastSeenId };
        }

        const docs = await collection.find(query).sort({ _id: 1 }).toArray();

        for (const doc of docs) {
          if (doc._id instanceof ObjectId) {
            lastSeenId = doc._id;
          }

          const payload = normalizeLog(doc);
          if (matchesFilters(payload, filters) && socket.readyState === OPEN) {
            socket.send(JSON.stringify(payload));
          }
        }
      } catch (error) {
        if (socket.readyState === OPEN) {
          socket.send(JSON.stringify({ message: error.message || 'stream error' }));
        }
      }

      await new Promise((resolve) => {
        setTimeout(resolve, POLL_INTERVAL_MS);
      });
    }
  };

  pump().catch(() => {
    if (socket.readyState === OPEN) {
      socket.close();
    }
  });
});

async function start() {
  mongoClient = new MongoClient(MONGO_URI);
  await mongoClient.connect();
  collection = mongoClient.db(MONGO_DATABASE).collection(MONGO_COLLECTION);

  server.listen(PORT, HOST, () => {
    console.log(`[logs-service] listening on http://${HOST}:${PORT}`);
  });
}

start().catch((error) => {
  console.error('[logs-service] startup failed:', error);
  process.exit(1);
});
