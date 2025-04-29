import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createProxyMiddleware } from 'http-proxy-middleware';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

/**
 * Configure API proxy to backend services
 * In Kubernetes, this will use the service name (backend-service)
 */
const backendServiceUrl = process.env['BACKEND_SERVICE_URL'] || 'http://backend-service';
console.log(`Configuring API proxy to backend at: ${backendServiceUrl}`);

// API proxy middleware - routes /api/* requests to backend service
app.use('/api', createProxyMiddleware({
  target: backendServiceUrl,
  pathRewrite: {'^/api': ''},
  changeOrigin: true,
}));

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Ensure static files from the public folder are served before Angular routes
 */
app.use('/public', express.static(resolve(serverDistFolder, '../public'), {
  maxAge: '1y',
  index: false,
  redirect: false,
}));

/**
 * Health check endpoints
 */
app.get('/healthz', (req, res) => {
  res.setHeader('content-type', 'text/plain');
  res.status(200).send('Service is operational');
});

app.get('/healthz/ready', (req, res) => {
  res.setHeader('content-type', 'text/plain');
  res.setHeader('readiness', 'healthy');
  res.status(200).send('Service is ready');
});

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use('/**', (req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] ?? 4000;
  app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
    console.log(`API proxy configured to: ${backendServiceUrl}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
