const express = require('express');
const cors = require('cors');
const swaggerUi = require('swagger-ui-express');
require('dotenv').config();

const authRoutes = require('./routes/auth');
const dataRoutes = require('./routes/data');
const healthRoutes = require('./routes/health');
const handsRoutes = require('./routes/hands');
const logsRoutes = require('./routes/logs');
const openApiDocument = require('./swagger');
const { handleWsUpgrade } = require('./routes/logs');
const { importantLogsMiddleware } = require('./middlewares/importantLogsMiddleware');

const app = express();

app.use(cors());
app.use(express.json());
app.use(importantLogsMiddleware);

app.get('/openapi.json', (_req, res) => {
    res.json(openApiDocument);
});

app.use('/swagger', swaggerUi.serve, swaggerUi.setup(openApiDocument, {
    explorer: true,
    customSiteTitle: 'FigurasQE Gateway Swagger'
}));

app.use('/auth', authRoutes);
app.use('/data', dataRoutes);
app.use('/health', healthRoutes);
app.use('/hands', handsRoutes);
app.use('/logs', logsRoutes);

const PORT = process.env.PORT || 3000;

const server = app.listen(PORT, () => {
    console.log(`Gateway running on port ${PORT}`);
});

server.on('upgrade', handleWsUpgrade);