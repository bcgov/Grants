const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const { resolve } = require('path');
const rateLimit = require('express-rate-limit');

const rateLimitMax = process.env.RATE_LIMIT_MAX || 1000;
const rateLimitWindow = process.env.RATE_LIMIT_WINDOW_MS || (10 * 60 * 1000); // 10 mins

const app = express();
const port = process.env.PORT || 4000;
const enableProxy = process.env.ENABLE_API_PROXY === 'true';
const backendServiceUrl = process.env.BACKEND_SERVICE_URL || 'http://backend:5100';

// Configure Express to trust proxy headers properly for container environments
// This tells Express to trust the first proxy (OpenShift router) but not beyond that
app.set('trust proxy', 1);

// Rate limiter for catch-all route serving index.html
const catchAllLimiter = rateLimit({
  windowMs: rateLimitWindow,
  max: rateLimitMax,
  standardHeaders: true, // Return rate limit info in the `RateLimit-*` headers
  legacyHeaders: false, // Disable the `X-RateLimit-*` headers
  // Use a more specific key generator for container environments
  keyGenerator: (req) => {
    // In container environments, use X-Forwarded-For if available, otherwise fall back to connection IP
    return req.ip || req.connection.remoteAddress || 'unknown';
  },
  message: {
    error: 'Too many requests from this IP, please try again later.'
  }
});

console.log(`Starting server...`);

if (enableProxy) {
  console.log(`Configuring API proxy to backend at: ${backendServiceUrl}`);
  
  // API proxy middleware - routes /api/* requests to backend service
  app.use('/api', createProxyMiddleware({
    target: backendServiceUrl,
    pathRewrite: {'^/api': ''},
    changeOrigin: true,
    onError: (err, req, res) => {
      console.error('Proxy error:', err.message);
    },
    onProxyReq: (proxyReq, req, res) => {
      console.log(`Proxying ${req.method} ${req.url} to ${backendServiceUrl}`);
    }
  }));
  console.log(`API proxy enabled - routing /api/* to ${backendServiceUrl}`);
} else {
  console.log(`API proxy disabled - using platform-level routing`);
}

// Health check endpoints
app.get('/healthz', (req, res) => {
  res.setHeader('content-type', 'text/plain');
  res.status(200).send('Service is operational');
});

app.get('/healthz/ready', (req, res) => {
  res.setHeader('content-type', 'text/plain');
  res.setHeader('readiness', 'healthy');
  res.status(200).send('Service is ready');
});

// Serve static files from dist/frontend/browser
const staticPath = resolve(__dirname, 'dist/frontend/browser');
console.log(`Serving static files from: ${staticPath}`);

app.use(express.static(staticPath, {
  maxAge: '1y'
}));

// Handle Angular routing - serve index.html for all routes
app.get('*', catchAllLimiter, (req, res) => {
  console.log(`Request: ${req.method} ${req.url}`);
  res.sendFile(resolve(staticPath, 'index.html'));
});

app.listen(port, () => {
  console.log(`Server listening on http://localhost:${port}`);
  if (enableProxy) {
    console.log(`API proxy: ENABLED - /api/* → ${backendServiceUrl}`);
  } else {
    console.log(`API proxy: DISABLED - relying on platform routing`);
  }
});
