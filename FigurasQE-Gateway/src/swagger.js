const openApiDocument = {
    openapi: '3.0.3',
    info: {
        title: 'FigurasQE Gateway API',
        version: '1.0.0',
        description: 'API Gateway para autenticacion, datos, health checks, manos y logs.'
    },
    servers: [
        { url: 'http://localhost:3000', description: 'Gateway local por defecto' }
    ],
    tags: [
        { name: 'Auth' },
        { name: 'Data' },
        { name: 'Health' },
        { name: 'Hands' },
        { name: 'Logs' }
    ],
    components: {
        securitySchemes: {
            bearerAuth: {
                type: 'http',
                scheme: 'bearer',
                bearerFormat: 'JWT'
            }
        },
        schemas: {
            LoginRequest: {
                type: 'object',
                properties: {
                    email: { type: 'string', format: 'email' },
                    password: { type: 'string' }
                },
                required: ['email', 'password']
            },
            RegisterRequest: {
                type: 'object',
                properties: {
                    name: { type: 'string' },
                    lastName: { type: 'string' },
                    secondLastName: { type: 'string', nullable: true },
                    birthDate: { type: 'string', format: 'date' },
                    genre: { type: 'string', enum: ['M', 'F', 'O'] },
                    country: { type: 'string', minLength: 2, maxLength: 2 },
                    email: { type: 'string', format: 'email' },
                    password: { type: 'string' },
                    role: { type: 'string', enum: ['student', 'tutor'] }
                },
                required: ['name', 'lastName', 'birthDate', 'genre', 'country', 'email', 'password', 'role']
            },
            AuthResponse: {
                type: 'object',
                properties: {
                    token: { type: 'string' },
                    role: { type: 'string' },
                    admin: { type: 'boolean', nullable: true }
                }
            },
            ErrorResponse: {
                type: 'object',
                properties: {
                    message: { type: 'string' },
                    error: { type: 'string', nullable: true },
                    errors: {
                        type: 'object',
                        additionalProperties: true,
                        nullable: true
                    },
                    details: {
                        type: 'object',
                        additionalProperties: true,
                        nullable: true
                    }
                }
            },
            HealthResponse: {
                type: 'object',
                properties: {
                    service: { type: 'string' },
                    status: { type: 'string' },
                    rabbitmq: { type: 'string', nullable: true },
                    database: { type: 'string', nullable: true },
                    mongo: { type: 'string', nullable: true },
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
            HandDetectionResponse: {
                type: 'object',
                properties: {
                    left: { type: 'integer' },
                    right: { type: 'integer' },
                    total: { type: 'integer' },
                    hands: { type: 'integer' }
                }
            }
        }
    },
    paths: {
        '/auth/login': {
            post: {
                tags: ['Auth'],
                summary: 'Iniciar sesion como student o tutor',
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { $ref: '#/components/schemas/LoginRequest' }
                        }
                    }
                },
                responses: {
                    '200': {
                        description: 'Login exitoso',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/AuthResponse' }
                            }
                        }
                    },
                    '401': {
                        description: 'Credenciales invalidas',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/ErrorResponse' }
                            }
                        }
                    }
                }
            }
        },
        '/auth/admin/login': {
            post: {
                tags: ['Auth'],
                summary: 'Iniciar sesion como admin',
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { $ref: '#/components/schemas/LoginRequest' }
                        }
                    }
                },
                responses: {
                    '200': {
                        description: 'Login exitoso',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/AuthResponse' }
                            }
                        }
                    },
                    '401': {
                        description: 'Credenciales invalidas',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/ErrorResponse' }
                            }
                        }
                    }
                }
            }
        },
        '/auth/register': {
            post: {
                tags: ['Auth'],
                summary: 'Registrar un usuario y devolver JWT',
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { $ref: '#/components/schemas/RegisterRequest' }
                        }
                    }
                },
                responses: {
                    '200': {
                        description: 'Registro exitoso',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/AuthResponse' }
                            }
                        }
                    },
                    '400': {
                        description: 'Error de validacion',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/ErrorResponse' }
                            }
                        }
                    }
                }
            }
        },
        '/data/admins': {
            get: {
                tags: ['Data'],
                summary: 'Listar admins',
                security: [{ bearerAuth: [] }],
                responses: {
                    '200': { description: 'Listado de admins' },
                    '401': { description: 'No autorizado' }
                }
            },
            post: {
                tags: ['Data'],
                summary: 'Crear admin',
                security: [{ bearerAuth: [] }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '201': { description: 'Admin creado' },
                    '400': { description: 'Solicitud invalida' }
                }
            }
        },
        '/data/admins/{id}': {
            get: {
                tags: ['Data'],
                summary: 'Obtener admin por id',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                responses: {
                    '200': { description: 'Admin encontrado' },
                    '404': { description: 'No encontrado' }
                }
            },
            put: {
                tags: ['Data'],
                summary: 'Actualizar admin',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '200': { description: 'Admin actualizado' },
                    '400': { description: 'Solicitud invalida' }
                }
            },
            delete: {
                tags: ['Data'],
                summary: 'Eliminar admin',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                responses: {
                    '200': { description: 'Admin eliminado' },
                    '404': { description: 'No encontrado' }
                }
            }
        },
        '/data/students': {
            get: {
                tags: ['Data'],
                summary: 'Listar estudiantes',
                security: [{ bearerAuth: [] }],
                responses: {
                    '200': { description: 'Listado de estudiantes' }
                }
            }
        },
        '/data/students/{id}': {
            get: {
                tags: ['Data'],
                summary: 'Obtener estudiante por id',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                responses: {
                    '200': { description: 'Estudiante encontrado' },
                    '404': { description: 'No encontrado' }
                }
            },
            put: {
                tags: ['Data'],
                summary: 'Actualizar estudiante',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '200': { description: 'Estudiante actualizado' },
                    '400': { description: 'Solicitud invalida' }
                }
            }
        },
        '/data/students/{id}/sessions': {
            get: {
                tags: ['Data'],
                summary: 'Listar sesiones por estudiante',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                responses: {
                    '200': { description: 'Listado de sesiones' }
                }
            }
        },
        '/data/sessions': {
            post: {
                tags: ['Data'],
                summary: 'Crear sesion',
                security: [{ bearerAuth: [] }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '201': { description: 'Sesion creada' },
                    '400': { description: 'Solicitud invalida' }
                }
            }
        },
        '/data/level-results': {
            post: {
                tags: ['Data'],
                summary: 'Crear resultado de nivel',
                security: [{ bearerAuth: [] }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '201': { description: 'Resultado creado' },
                    '400': { description: 'Solicitud invalida' }
                }
            }
        },
        '/data/tutors/{id}': {
            get: {
                tags: ['Data'],
                summary: 'Obtener tutor por id',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                responses: {
                    '200': { description: 'Tutor encontrado' },
                    '404': { description: 'No encontrado' }
                }
            },
            put: {
                tags: ['Data'],
                summary: 'Actualizar tutor',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '200': { description: 'Tutor actualizado' },
                    '400': { description: 'Solicitud invalida' }
                }
            }
        },
        '/data/tutors/{id}/students': {
            get: {
                tags: ['Data'],
                summary: 'Listar estudiantes de un tutor',
                security: [{ bearerAuth: [] }],
                parameters: [{ name: 'id', in: 'path', required: true, schema: { type: 'integer' } }],
                responses: {
                    '200': { description: 'Listado de estudiantes del tutor' }
                }
            }
        },
        '/data/tutors/assign-student': {
            post: {
                tags: ['Data'],
                summary: 'Asignar estudiante a tutor',
                security: [{ bearerAuth: [] }],
                requestBody: {
                    required: true,
                    content: {
                        'application/json': {
                            schema: { type: 'object', additionalProperties: true }
                        }
                    }
                },
                responses: {
                    '200': { description: 'Asignacion realizada' },
                    '400': { description: 'Solicitud invalida' }
                }
            }
        },
        '/data/dashboard/summary': {
            get: {
                tags: ['Data'],
                summary: 'Obtener resumen del dashboard',
                security: [{ bearerAuth: [] }],
                responses: {
                    '200': { description: 'Resumen del dashboard' },
                    '403': { description: 'Prohibido' }
                }
            }
        },
        '/health': {
            get: {
                tags: ['Health'],
                summary: 'Health local del gateway',
                responses: {
                    '200': {
                        description: 'Gateway disponible',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/HealthResponse' }
                            }
                        }
                    }
                }
            }
        },
        '/health/{serviceName}': {
            get: {
                tags: ['Health'],
                summary: 'Health de un servicio upstream',
                parameters: [{
                    name: 'serviceName',
                    in: 'path',
                    required: true,
                    schema: {
                        type: 'string',
                        enum: ['gateway', 'auth', 'data', 'frontend', 'postgres', 'logs', 'mongo', 'rabbit-listener']
                    }
                }],
                responses: {
                    '200': {
                        description: 'Estado del servicio',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/HealthResponse' }
                            }
                        }
                    },
                    '404': { description: 'Servicio desconocido' },
                    '503': { description: 'Servicio no configurado o no disponible' }
                }
            }
        },
        '/hands': {
            post: {
                tags: ['Hands'],
                summary: 'Enviar imagen para deteccion de manos',
                requestBody: {
                    required: true,
                    content: {
                        'multipart/form-data': {
                            schema: {
                                type: 'object',
                                properties: {
                                    image: {
                                        type: 'string',
                                        format: 'binary'
                                    }
                                },
                                required: ['image']
                            }
                        }
                    }
                },
                responses: {
                    '200': {
                        description: 'Deteccion exitosa',
                        content: {
                            'application/json': {
                                schema: { $ref: '#/components/schemas/HandDetectionResponse' }
                            }
                        }
                    },
                    '400': { description: 'Imagen faltante' },
                    '504': { description: 'Timeout del backend gRPC' }
                }
            }
        },
        '/logs/api/logs': {
            get: {
                tags: ['Logs'],
                summary: 'Consultar logs agregados',
                security: [{ bearerAuth: [] }],
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
                    '503': { description: 'Servicio de logs no disponible' }
                }
            }
        },
        '/logs/api/logs/service/{serviceName}': {
            get: {
                tags: ['Logs'],
                summary: 'Consultar logs por servicio',
                security: [{ bearerAuth: [] }],
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
                        description: 'Listado de logs del servicio',
                        content: {
                            'application/json': {
                                schema: {
                                    type: 'array',
                                    items: { $ref: '#/components/schemas/LogEntry' }
                                }
                            }
                        }
                    },
                    '503': { description: 'Servicio de logs no disponible' }
                }
            }
        }
    }
};

module.exports = openApiDocument;