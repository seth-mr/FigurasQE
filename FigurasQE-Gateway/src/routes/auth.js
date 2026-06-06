const express = require('express');
const axios = require('axios');

const router = express.Router();

const AUTH_SERVICE = process.env.AUTH_SERVICE;

router.post('/login', async (req, res) => {
    try {
        const response = await axios.post(
            `${AUTH_SERVICE}/auth/login`,
            req.body
        );

        res.status(response.status).json(response.data);
    } catch (error) {
        const status = error.response?.status || 500;
        const data = error.response?.data;
        
        res.status(status).json({
            message: data?.message || data?.error || "Authentication Service Error",
            errors: data?.errors || null
        });
    }
});

router.post('/admin/login', async (req, res) => {
    try {
        const response = await axios.post(
            `${AUTH_SERVICE}/auth/admin/login`,
            req.body
        );

        res.status(response.status).json(response.data);
    } catch (error) {
        const status = error.response?.status || 500;
        const data = error.response?.data;

        res.status(status).json({
            message: data?.message || data?.error || "Authentication Service Error",
            errors: data?.errors || null
        });
    }
});

router.post('/register', async (req, res) => {
    try {
        const response = await axios.post(
            `${AUTH_SERVICE}/auth/register`,
            req.body
        );

        res.status(response.status).json(response.data);
    } catch (error) {
        const status = error.response?.status || 500;
        const data = error.response?.data;

        res.status(status).json({
            message: data?.message || data?.error || "Authentication Service Error",
            errors: data?.errors || null
        });
    }
});

module.exports = router;