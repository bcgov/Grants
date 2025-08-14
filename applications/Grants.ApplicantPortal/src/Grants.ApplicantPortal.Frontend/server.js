const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const { resolve } = require('path');

const app = express();
const port = process.env.PORT || 4000;
const enableProxy = process.env.ENABLE_API_PROXY === 'true';
const backendServiceUrl = process.env.BACKEND_SERVICE_URL || 'http://backend:5100';

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
} else {
  console.log(`API proxy disabled - using direct routing or platform-level routing`);
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
app.get('*', (req, res) => {
  console.log(`Request: ${req.method} ${req.url}`);
  res.sendFile(resolve(staticPath, 'index.html'));
});

app.listen(port, () => {
  console.log(`SPA server listening on http://localhost:${port}`);
  console.log(`API proxy configured to: ${backendServiceUrl}`);
});
