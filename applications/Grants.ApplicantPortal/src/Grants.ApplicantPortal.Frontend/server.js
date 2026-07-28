const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const { resolve, relative, sep } = require('node:path');
const rateLimit = require('express-rate-limit');
const fs = require('node:fs');

const rateLimitMax = process.env.RATE_LIMIT_MAX || 1000;
const rateLimitWindow = process.env.RATE_LIMIT_WINDOW_MS || (10 * 60 * 1000); // 10 mins

const app = express();

// Strip CR/LF and other control characters from user-controlled values before
// writing them to logs, to prevent log injection/forging (CWE-117). Filters by
// code point instead of a control-character regex class so static analyzers
// don't flag embedded control characters in the pattern itself.
function sanitizeForLog(value) {
  let sanitized = '';
  for (const char of String(value)) {
    const codePoint = char.codePointAt(0);
    sanitized += (codePoint <= 0x1F || codePoint === 0x7F) ? ' ' : char;
  }
  return sanitized;
}

// Remove server identification headers
app.disable('x-powered-by');
const port = process.env.PORT || 4200;
const enableProxy = process.env.ENABLE_API_PROXY === 'true';
const backendServiceUrl = process.env.BACKEND_SERVICE_URL || 'http://backend:5100';

// Environment variables for runtime substitution
const envVars = {
  KEYCLOAK__AUTHSERVERURL: process.env.KEYCLOAK__AUTHSERVERURL || 'https://dev.loginproxy.gov.bc.ca/auth',
  KEYCLOAK__REALM: process.env.KEYCLOAK__REALM || 'standard',
  KEYCLOAK__RESOURCE: process.env.KEYCLOAK__RESOURCE || 'grants-portal-5361',
  MATOMO__URL: process.env.MATOMO__URL || '//dev-analytics-matomo.apps.silver.devops.gov.bc.ca/',
  MATOMO__SITEID: process.env.MATOMO__SITEID || '2'
};

// Configure Express to trust proxy headers properly for container environments
// This tells Express to trust the first proxy (OpenShift router) but not beyond that
app.set('trust proxy', 1);

// Global request logging middleware
app.use((req, res, next) => {
  console.log(`[${new Date().toISOString()}] ${sanitizeForLog(req.method)} ${sanitizeForLog(req.url)}`);
  next();
});

// Rate limiter for routes that hit the filesystem per request (JS bundle
// substitution and the SPA catch-all serving index.html)
const staticFileLimiter = rateLimit({
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
    timeout: 30000,
    proxyTimeout: 60000,
    onError: (err, req, res) => {
      console.error(`Proxy error [${req.method} ${req.url}]:`, err.message);
      if (!res.headersSent) {
        res.status(502).json({ error: 'Backend service unavailable' });
      }
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
    const escapedPlaceholder = placeholder.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`);
    const regularMatches = content.match(new RegExp(escapedPlaceholder, 'g'));
    if (regularMatches) {
      console.log(`Found ${regularMatches.length} regular placeholder(s) for ${key}: ${placeholder}`);
      result = result.replace(new RegExp(escapedPlaceholder, 'g'), value);
      substitutionsMade = true;
    }
    
    // Handle URL-encoded ${VARIABLE} pattern (%7B = {, %7D = })
    const urlEncodedPlaceholder = `$%7B${key}%7D`;
    const urlMatches = content.match(new RegExp(urlEncodedPlaceholder.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`), 'g'));
    if (urlMatches) {
      console.log(`Found ${urlMatches.length} URL-encoded placeholder(s) for ${key}: ${urlEncodedPlaceholder}`);
      result = result.replace(new RegExp(urlEncodedPlaceholder.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`), 'g'), value);
      substitutionsMade = true;
    }
    
    // Handle mixed case URL encoding
    const urlEncodedPlaceholderLower = `$%7b${key}%7d`;
    const lowerMatches = content.match(new RegExp(urlEncodedPlaceholderLower.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`), 'g'));
    if (lowerMatches) {
      console.log(`Found ${lowerMatches.length} lowercase URL-encoded placeholder(s) for ${key}: ${urlEncodedPlaceholderLower}`);
      result = result.replace(new RegExp(urlEncodedPlaceholderLower.replace(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`), 'g'), value);
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

// Build an allow-list of the actual .js files under staticPath once at startup,
// mapping each request-style path (e.g. "/main.js") to its real absolute path.
// Requests are matched against this known-good map instead of resolving
// user-controlled request paths against the filesystem, so no fs call is ever
// made with a path derived from user input (CWE-22).
function buildJsFileAllowList(dir, base = dir) {
  const allowList = new Map();
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = resolve(dir, entry.name);
    if (entry.isDirectory()) {
      for (const [key, value] of buildJsFileAllowList(fullPath, base)) {
        allowList.set(key, value);
      }
    } else if (entry.isFile() && entry.name.endsWith('.js')) {
      const requestPath = '/' + relative(base, fullPath).split(sep).join('/');
      allowList.set(requestPath, fullPath);
    }
  }
  return allowList;
}

const jsFileAllowList = buildJsFileAllowList(staticPath);

// Custom middleware for JavaScript files that need environment variable substitution
// This MUST come BEFORE the static file middleware
app.get('*.js', staticFileLimiter, (req, res, next) => {
  console.log(`JavaScript request: ${sanitizeForLog(req.path)}`);

  const filePath = jsFileAllowList.get(req.path);
  if (!filePath) {
    console.log('Rejected JavaScript request not in known file allow-list');
    return next();
  }

  console.log(`File exists, reading for substitution: ${sanitizeForLog(filePath)}`);
  fs.readFile(filePath, 'utf8', (err, data) => {
    if (err) {
      console.error('Error reading JS file:', sanitizeForLog(filePath), err);
      return next();
    }

    console.log(`Processing JS file for env substitution: ${sanitizeForLog(req.path)} (${data.length} characters)`);
    const substitutedContent = substituteEnvironmentVariables(data);

    // Log if substitution occurred
    if (substitutedContent !== data) {
      console.log('Environment variable substitution applied to:', sanitizeForLog(req.path));
    } else {
      console.log('No substitutions needed for:', sanitizeForLog(req.path));
    }

    res.setHeader('Content-Type', 'application/javascript');
    res.setHeader('Cache-Control', 'no-cache, no-store, must-revalidate');
    res.setHeader('Pragma', 'no-cache');
    res.setHeader('Expires', '0');
    res.send(substitutedContent);
  });
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
app.get('*', staticFileLimiter, (req, res) => {
  console.log(`Request: ${sanitizeForLog(req.method)} ${sanitizeForLog(req.url)}`);
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
  console.log(`Max HTTP header size: ${process.env.NODE_OPTIONS?.includes('--max-http-header-size') ? 'Custom' : 'Default (8KB)'}`);
  if (enableProxy) {
    console.log(`API proxy: ENABLED - /api/* → ${backendServiceUrl}`);
  } else {
    console.log(`API proxy: DISABLED - relying on platform routing`);
  }
});
