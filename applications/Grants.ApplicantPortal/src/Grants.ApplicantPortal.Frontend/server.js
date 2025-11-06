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

// Global request logging middleware
app.use((req, res, next) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.url}`);
  next();
});

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
console.log('Environment variables:');
console.log('  PORT:', port);
console.log('  ENABLE_API_PROXY:', enableProxy);
console.log('  BACKEND_SERVICE_URL:', backendServiceUrl);
console.log('  KEYCLOAK__AUTHSERVERURL:', envVars.KEYCLOAK__AUTHSERVERURL);
console.log('  KEYCLOAK__REALM:', envVars.KEYCLOAK__REALM);
console.log('  KEYCLOAK__RESOURCE:', envVars.KEYCLOAK__RESOURCE);

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
  let substitutionsMade = false;
  
  Object.keys(envVars).forEach(key => {
    const value = envVars[key];
    
    // Handle regular ${VARIABLE} pattern
    const placeholder = `\${${key}}`;
    const escapedPlaceholder = placeholder.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regularMatches = content.match(new RegExp(escapedPlaceholder, 'g'));
    if (regularMatches) {
      console.log(`Found ${regularMatches.length} regular placeholder(s) for ${key}: ${placeholder}`);
      result = result.replace(new RegExp(escapedPlaceholder, 'g'), value);
      substitutionsMade = true;
    }
    
    // Handle URL-encoded ${VARIABLE} pattern (%7B = {, %7D = })
    const urlEncodedPlaceholder = `$%7B${key}%7D`;
    const urlMatches = content.match(new RegExp(urlEncodedPlaceholder.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g'));
    if (urlMatches) {
      console.log(`Found ${urlMatches.length} URL-encoded placeholder(s) for ${key}: ${urlEncodedPlaceholder}`);
      result = result.replace(new RegExp(urlEncodedPlaceholder.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g'), value);
      substitutionsMade = true;
    }
    
    // Handle mixed case URL encoding
    const urlEncodedPlaceholderLower = `$%7b${key}%7d`;
    const lowerMatches = content.match(new RegExp(urlEncodedPlaceholderLower.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g'));
    if (lowerMatches) {
      console.log(`Found ${lowerMatches.length} lowercase URL-encoded placeholder(s) for ${key}: ${urlEncodedPlaceholderLower}`);
      result = result.replace(new RegExp(urlEncodedPlaceholderLower.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'g'), value);
      substitutionsMade = true;
    }
  });
  
  if (substitutionsMade) {
    console.log('Environment variable substitutions completed');
  }
  
  return result;
}

// Serve static files from dist/frontend/browser
const staticPath = resolve(__dirname, 'dist/frontend/browser');
console.log(`Serving static files from: ${staticPath}`);

// Custom middleware for JavaScript files that need environment variable substitution
// This MUST come BEFORE the static file middleware
app.get('*.js', (req, res, next) => {
  const filePath = resolve(staticPath, req.path.substring(1));
  
  console.log(`JavaScript request: ${req.path}`);
  console.log(`Looking for file: ${filePath}`);
  
  if (fs.existsSync(filePath)) {
    console.log(`File exists, reading for substitution: ${filePath}`);
    fs.readFile(filePath, 'utf8', (err, data) => {
      if (err) {
        console.error('Error reading JS file:', filePath, err);
        return next();
      }
      
      console.log(`Processing JS file for env substitution: ${req.path} (${data.length} characters)`);
      const substitutedContent = substituteEnvironmentVariables(data);
      
      // Log if substitution occurred
      if (substitutedContent !== data) {
        console.log('Environment variable substitution applied to:', req.path);
      } else {
        console.log('No substitutions needed for:', req.path);
      }
      
      res.setHeader('Content-Type', 'application/javascript');
      res.setHeader('Cache-Control', 'no-cache, no-store, must-revalidate');
      res.setHeader('Pragma', 'no-cache');
      res.setHeader('Expires', '0');
      res.send(substitutedContent);
    });
  } else {
    console.log(`File does not exist: ${filePath}`);
    next();
  }
});

// Static file middleware - serves all other files except .js (which are handled above)
app.use(express.static(staticPath, {
  maxAge: '1y',
  setHeaders: (res, path) => {
    if (path.endsWith('.js')) {
      // This should not happen since .js files are handled above
      console.log('WARNING: JavaScript file served by static middleware:', path);
      res.setHeader('Cache-Control', 'no-cache');
    }
  }
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
