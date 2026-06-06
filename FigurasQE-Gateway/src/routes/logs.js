const express = require('express');
const axios = require('axios');
const { WebSocket, WebSocketServer } = require('ws');

const router = express.Router();

const LOGS_SERVICE = process.env.LOGS_SERVICE || 'http://localhost:5186';

const isValidCloseCode = (code) =>
    Number.isInteger(code) && (code === 1000 || (code >= 3000 && code <= 4999));

const safeClose = (socket, code, reason) => {
    if (socket.readyState !== WebSocket.OPEN && socket.readyState !== WebSocket.CLOSING) {
        return;
    }

    if (isValidCloseCode(code)) {
        socket.close(code, reason);
        return;
    }

    socket.close();
};

// HTTP: GET /logs/api/logs
router.get('/api/logs', async (req, res) => {
    try {
        const response = await axios.get(`${LOGS_SERVICE}/api/logs`, {
            headers: { Authorization: req.headers.authorization },
            params: req.query,
        });
        res.status(response.status).json(response.data);
    } catch (error) {
        const status = error.response?.status || 503;
        const data = error.response?.data;
        res.status(status).json({ message: data?.detail || data?.message || 'Logs Service Error' });
    }
});

// HTTP: GET /logs/api/logs/service/:serviceName
router.get('/api/logs/service/:serviceName', async (req, res) => {
    try {
        const response = await axios.get(
            `${LOGS_SERVICE}/api/logs/service/${encodeURIComponent(req.params.serviceName)}`,
            {
                headers: { Authorization: req.headers.authorization },
                params: req.query,
            }
        );
        res.status(response.status).json(response.data);
    } catch (error) {
        const status = error.response?.status || 503;
        const data = error.response?.data;
        res.status(status).json({ message: data?.detail || data?.message || 'Logs Service Error' });
    }
});

// WebSocket upgrade handler — llamado desde server.js para /ws/logs
function handleWsUpgrade(req, socket, head) {
    const parsedUrl = new URL(req.url, 'http://localhost');
    if (parsedUrl.pathname !== '/ws/logs') return;

    const targetBase = LOGS_SERVICE.replace(/^https/, 'wss').replace(/^http/, 'ws');
    const targetUrl = `${targetBase.replace(/\/$/, '')}/ws/logs${parsedUrl.search || ''}`;

    const wss = new WebSocketServer({ noServer: true });

    wss.handleUpgrade(req, socket, head, (client) => {
        const upstream = new WebSocket(targetUrl);

        upstream.on('open', () => {
            client.on('message', (data, isBinary) => {
                if (upstream.readyState === WebSocket.OPEN) {
                    upstream.send(data, { binary: isBinary });
                }
            });
            client.on('close', (code, reason) => safeClose(upstream, code, reason?.toString() || ''));
            client.on('error', () => upstream.terminate());
        });

        upstream.on('message', (data, isBinary) => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(data, { binary: isBinary });
            }
        });

        upstream.on('close', (code, reason) => {
            if (client.readyState === WebSocket.OPEN) {
                safeClose(client, code, reason?.toString() || '');
            }
        });

        upstream.on('error', (err) => {
            console.error('[logs-ws] upstream error:', err.message);
            client.terminate();
        });
    });
}

module.exports = router;
module.exports.handleWsUpgrade = handleWsUpgrade;
