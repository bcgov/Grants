const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const { resolve } = require('path');
const rateLimit = require('express-rate-limit');
const fs = require('fs');

const rateLimitMax = process.env.RATE_LIMIT_MAX || 1000;
const rateLimitWindow = process.env.RATE_LIMIT_WINDOW_MS || (10 * 60 * 1000); // 10 mins

const app = express();
const port = process.env.PORT || 4200;
const enableProxy = process.env.ENABLE_API_PROXY === 'true';
const backendServiceUrl = process.env.BACKEND_SERVICE_URL || 'http://backend:5100';

// Environment variables for runtime substitution
const envVars = {
  KEYCLOAK__AUTHSERVERURL: process.env.KEYCLOAK__AUTHSERVERURL || 'https://dev.loginproxy.gov.bc.ca/auth',
  KEYCLOAK__REALM: process.env.KEYCLOAK__REALM || 'standard',
  KEYCLOAK__RESOURCE: process.env.KEYCLOAK__RESOURCE || 'grants-portal-5361',
  KEYCLOAK__CREDENTIALS__SECRET: process.env.KEYCLOAK__CREDENTIALS__SECRET || 'placeholder-client-secret'
};

// Configure Express to trust proxy headers properly for container environments
// This tells Express to trust the first proxy (OpenShift router) but not beyond that
app.set('trust proxy', 1);

// Rate limiter for catch-all route serving index.html
const catchAllLimiter = rateLimit({
  windowMs: rateLimitWindow,
  max: rateLimitMax,
  standardHeaders: true, // Return rate limit info in the `RateLimit-*` headers
  legacyHeaders: false, // Disable the `X-RateLimit-*` headers
  // Remove custom keyGenerator - let express-rate-limit handle IP detection properly
  // This automatically handles IPv4, IPv6, and proxy headers correctly
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

// Function to substitute environment variables in file content
function substituteEnvironmentVariables(content) {
  let result = content;
  Object.keys(envVars).forEach(key => {
    const placeholder = `\${${key}}`;
    result = result.replace(new RegExp(placeholder.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g'), envVars[key]);
  });
  return result;
}

// Serve static files from dist/frontend/browser
const staticPath = resolve(__dirname, 'dist/frontend/browser');
console.log(`Serving static files from: ${staticPath}`);

// Custom middleware for JavaScript files that need environment variable substitution
app.get('*.js', (req, res, next) => {
  const filePath = resolve(staticPath, req.path.substring(1));
  
  if (fs.existsSync(filePath)) {
    fs.readFile(filePath, 'utf8', (err, data) => {
      if (err) {
        return next();
      }
      
      const substitutedContent = substituteEnvironmentVariables(data);
      res.setHeader('Content-Type', 'application/javascript');
      res.send(substitutedContent);
    });
  } else {
    next();
  }
});

app.use(express.static(staticPath, {
  maxAge: '1y'
}));

// Handle Angular routing - serve index.html for all routes
app.get('*', catchAllLimiter, (req, res) => {
  console.log(`Request: ${req.method} ${req.url}`);
  const indexPath = resolve(staticPath, 'index.html');
  
  // Read and substitute environment variables in index.html
  fs.readFile(indexPath, 'utf8', (err, data) => {
    if (err) {
      console.error('Error reading index.html:', err);
      return res.status(500).send('Internal Server Error');
    }
    
    const substitutedContent = substituteEnvironmentVariables(data);
    res.setHeader('Content-Type', 'text/html');
    res.send(substitutedContent);
  });
});

app.listen(port, () => {
  console.log(`Server listening on http://localhost:${port}`);
  if (enableProxy) {
    console.log(`API proxy: ENABLED - /api/* → ${backendServiceUrl}`);
  } else {
    console.log(`API proxy: DISABLED - relying on platform routing`);
  }
});
