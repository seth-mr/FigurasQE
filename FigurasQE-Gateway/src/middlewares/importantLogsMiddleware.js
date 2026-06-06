const { publishGatewayLog } = require('../services/rabbitLogPublisher');

const IMPORTANT_LOG_ENABLED = `${process.env.IMPORTANT_LOG_ENABLED || 'true'}`.toLowerCase() !== 'false';
const IMPORTANT_LOG_QUEUE_METHODS = (process.env.IMPORTANT_LOG_METHODS || 'POST,PUT,PATCH,DELETE')
    .split(',')
    .map((value) => value.trim().toUpperCase())
    .filter(Boolean);
const IMPORTANT_LOG_STATUS_MIN = Number(process.env.IMPORTANT_LOG_STATUS_MIN || 400);
const IMPORTANT_LOG_EXCLUDE_PREFIXES = (process.env.IMPORTANT_LOG_EXCLUDE_PREFIXES || '/health,/logs,/hands')
    .split(',')
    .map((value) => value.trim())
    .filter(Boolean);

function isExcludedPath(pathname) {
    return IMPORTANT_LOG_EXCLUDE_PREFIXES.some((prefix) => pathname.startsWith(prefix));
}

function normalizePath(path) {
    return `${path}`.split('?')[0] || '/';
}

function resolveServiceName(path) {
    const cleanPath = `${path}`.split('?')[0];

    if (cleanPath.startsWith('/auth')) {
        return 'auth-service';
    }

    if (cleanPath.startsWith('/data')) {
        return 'FiguerasQueEnse-anBD';
    }

    if (cleanPath.startsWith('/hands')) {
        return 'fingers-service';
    }

    return 'FigurasQE-Gateway';
}

function buildLogType({ method, statusCode }) {
    if (statusCode >= 400) {
        return 'error';
    }

    if (IMPORTANT_LOG_QUEUE_METHODS.includes(method)) {
        return 'success';
    }

    return 'event';
}

function buildEntitySnapshot(path) {
    const cleanPath = `${path}`.split('?')[0];
    const segments = cleanPath.split('/').filter(Boolean);

    if (!segments.length) {
        return { entityType: null, entityId: null };
    }

    // /data/admins/:id, /data/students/:id, /data/tutors/:id
    if (segments[0] === 'data' && segments[1]) {
        const entityMap = {
            admins: 'admin',
            students: 'student',
            tutors: 'tutor',
        };

        const entityType = entityMap[segments[1]] || null;
        const maybeId = Number.parseInt(segments[2], 10);

        return {
            entityType,
            entityId: Number.isNaN(maybeId) ? null : maybeId,
        };
    }

    return { entityType: null, entityId: null };
}

function parseJwtPayload(token) {
    if (!token || typeof token !== 'string') {
        return null;
    }

    const sections = token.split('.');
    if (sections.length < 2) {
        return null;
    }

    try {
        return JSON.parse(Buffer.from(sections[1], 'base64url').toString('utf8'));
    } catch {
        return null;
    }
}

function normalizeRoleToEntityType(role) {
    const normalized = `${role || ''}`.toLowerCase();
    if (normalized === 'admin') return 'admin';
    if (normalized === 'tutor') return 'tutor';
    if (normalized === 'student') return 'student';
    return null;
}

function isImportant({ method, statusCode, path }) {
    if (isExcludedPath(path)) {
        return false;
    }

    if (statusCode >= IMPORTANT_LOG_STATUS_MIN) {
        return true;
    }

    return IMPORTANT_LOG_QUEUE_METHODS.includes(method);
}

function getUserIdFromBearer(req) {
    const authHeader = req.headers.authorization;
    if (!authHeader || !authHeader.toLowerCase().startsWith('bearer ')) {
        return null;
    }

    const payload = parseJwtPayload(authHeader.slice(7));
    const sub = Number.parseInt(payload?.sub, 10);
    return Number.isNaN(sub) ? null : sub;
}

function extractEntityIdFromResponseBody(responseBody) {
    if (!responseBody || typeof responseBody !== 'object') {
        return null;
    }

    const candidates = [
        responseBody?.admin?.idAdmin,
        responseBody?.admin?.id,
        responseBody?.tutor?.idTutor,
        responseBody?.tutor?.id,
        responseBody?.student?.idStudent,
        responseBody?.student?.id,
        responseBody?.user?.id,
        responseBody?.idAdmin,
        responseBody?.idTutor,
        responseBody?.idStudent,
        responseBody?.id,
    ];

    for (const candidate of candidates) {
        const parsed = Number.parseInt(candidate, 10);
        if (!Number.isNaN(parsed)) {
            return parsed;
        }
    }

    return null;
}

function resolveAuthEntitySnapshot(path, responseBody) {
    const cleanPath = `${path}`.split('?')[0];
    if (!cleanPath.startsWith('/auth')) {
        return { entityType: null, entityId: null };
    }

    if (cleanPath === '/auth/admin/login') {
        return {
            entityType: 'admin',
            entityId: extractEntityIdFromResponseBody(responseBody),
        };
    }

    const responseTokenPayload = parseJwtPayload(responseBody?.token);
    const roleCandidate =
        responseBody?.role ||
        responseBody?.user?.role ||
        responseTokenPayload?.role ||
        null;

    const responseSub = Number.parseInt(responseTokenPayload?.sub, 10);

    return {
        entityType: normalizeRoleToEntityType(roleCandidate),
        entityId: Number.isNaN(responseSub)
            ? extractEntityIdFromResponseBody(responseBody)
            : responseSub,
    };
}

function importantLogsMiddleware(req, res, next) {
    if (!IMPORTANT_LOG_ENABLED) {
        return next();
    }

    const start = Date.now();

    const originalJson = res.json.bind(res);
    res.json = (body) => {
        res.locals.gatewayResponseBody = body;
        return originalJson(body);
    };

    res.on('finish', () => {
        const durationMs = Date.now() - start;
        const method = req.method.toUpperCase();
        const path = req.originalUrl || req.url || '/';
        const statusCode = res.statusCode;

        const normalizedPath = normalizePath(path);
        if (method === 'POST' && (normalizedPath === '/hands' || normalizedPath === '/hands/')) {
            return;
        }

        if (!isImportant({ method, statusCode, path })) {
            return;
        }

        const entitySnapshot = buildEntitySnapshot(path);
        const authSnapshot = resolveAuthEntitySnapshot(path, res.locals.gatewayResponseBody);
        const userIdFromToken = getUserIdFromBearer(req);
        const userIdFromResponse = extractEntityIdFromResponseBody(res.locals.gatewayResponseBody);
        const resolvedEntityId =
            entitySnapshot.entityId ??
            authSnapshot.entityId ??
            userIdFromToken ??
            userIdFromResponse ??
            null;
        const resolvedEntityType = entitySnapshot.entityType ?? authSnapshot.entityType ?? null;
        const resolvedServiceName = resolveServiceName(path);
        const logType = buildLogType({ method, statusCode });

        const event = {
            service: resolvedServiceName,
            type: logType,
            entityType: resolvedEntityType,
            entityId: resolvedEntityId,
            action: method,
            route: normalizedPath,
            statusCode,
            durationMs,
            timestamp: new Date().toISOString(),
        };

        publishGatewayLog(event).catch((error) => {
            console.error('[gateway-rabbit-log] failed to publish important log:', error.message);
        });
    });

    return next();
}

module.exports = {
    importantLogsMiddleware,
};
