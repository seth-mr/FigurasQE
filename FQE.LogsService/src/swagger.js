const openApiDocument = {
  openapi: '3.0.3',
  info: {
    title: 'FQE Logs Service API',
    version: '1.0.0',
    description: 'Servicio HTTP para consultar logs almacenados en MongoDB. El stream WebSocket vive en /ws/logs y no aparece en OpenAPI.'
  },
  servers: [
    { url: 'http://localhost:5186', description: 'Logs service local por defecto' }
  ],
  tags: [
    { name: 'Health' },
    { name: 'Logs' }
  ],
  components: {
    schemas: {
      HealthResponse: {
        type: 'object',
        properties: {
          service: { type: 'string' },
          status: { type: 'string' },
          mongo: { type: 'string' },
          message: { type: 'string', nullable: true }
        }
      },
      LogEntry: {
        type: 'object',
        properties: {
          id: { type: 'string' },
          service: { type: 'string' },
          route: { type: 'string', nullable: true },
          statusCode: { type: 'integer', nullable: true },
          durationMs: { type: 'integer', nullable: true },
          timestamp: { type: 'string', format: 'date-time' },
          type: { type: 'string' },
          entityType: { type: 'string', nullable: true },
          entityId: { type: 'integer', nullable: true },
          action: { type: 'string' }
        }
      },
      ErrorResponse: {
        type: 'object',
        properties: {
          message: { type: 'string' }
        }
      }
    }
  },
  paths: {
    '/health': {
      get: {
        tags: ['Health'],
        summary: 'Health check del servicio de logs',
        responses: {
          '200': {
            description: 'Estado del servicio',
            content: {
              'application/json': {
                schema: { $ref: '#/components/schemas/HealthResponse' }
              }
            }
          }
        }
      }
    },
    '/api/logs': {
      get: {
        tags: ['Logs'],
        summary: 'Consultar logs',
        parameters: [
          { name: 'service', in: 'query', schema: { type: 'string' } },
          { name: 'type', in: 'query', schema: { type: 'string' } },
          { name: 'entityType', in: 'query', schema: { type: 'string' } },
          { name: 'action', in: 'query', schema: { type: 'string' } },
          { name: 'statusClass', in: 'query', schema: { type: 'string' } },
          { name: 'route', in: 'query', schema: { type: 'string' } },
          { name: 'limit', in: 'query', schema: { type: 'integer', minimum: 1 } }
        ],
        responses: {
          '200': {
            description: 'Listado de logs',
            content: {
              'application/json': {
                schema: {
                  type: 'array',
                  items: { $ref: '#/components/schemas/LogEntry' }
                }
              }
            }
          },
          '500': {
            description: 'Error interno',
            content: {
              'application/json': {
                schema: { $ref: '#/components/schemas/ErrorResponse' }
              }
            }
          }
        }
      }
    },
    '/api/logs/service/{serviceName}': {
      get: {
        tags: ['Logs'],
        summary: 'Consultar logs filtrados por servicio',
        parameters: [
          { name: 'serviceName', in: 'path', required: true, schema: { type: 'string' } },
          { name: 'type', in: 'query', schema: { type: 'string' } },
          { name: 'entityType', in: 'query', schema: { type: 'string' } },
          { name: 'action', in: 'query', schema: { type: 'string' } },
          { name: 'statusClass', in: 'query', schema: { type: 'string' } },
          { name: 'route', in: 'query', schema: { type: 'string' } },
          { name: 'limit', in: 'query', schema: { type: 'integer', minimum: 1 } }
        ],
        responses: {
          '200': {
            description: 'Listado de logs filtrados',
            content: {
              'application/json': {
                schema: {
                  type: 'array',
                  items: { $ref: '#/components/schemas/LogEntry' }
                }
              }
            }
          },
          '500': {
            description: 'Error interno',
            content: {
              'application/json': {
                schema: { $ref: '#/components/schemas/ErrorResponse' }
              }
            }
          }
        }
      }
    }
  }
};

module.exports = openApiDocument;