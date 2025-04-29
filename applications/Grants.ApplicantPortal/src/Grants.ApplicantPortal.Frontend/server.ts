server.get('/healthz/ready', (req, res) => {
  res.setHeader('content-type', 'text/plain');
  res.setHeader('readiness', 'healthy');
  res.status(200).send('Service is ready');
});