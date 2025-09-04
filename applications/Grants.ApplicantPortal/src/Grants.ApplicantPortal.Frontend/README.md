# Grants Applicant Portal - Frontend

Angular-based frontend application.

## 🚀 Quick Start

### Local Development

```powershell
# Default mode (fastest for development)
npm start

# With API proxy (if backend is not running locally)
npm run start:with-proxy
```

### Docker Development

```powershell
# Build and run in Docker
docker build -t grants-frontend .
docker run -p 4000:4000 grants-frontend
```

## 📋 Architecture

### Server Architecture

| **File** | **Language** | **Purpose** | **Notes** |
|----------|-------------|-------------|-----------|
| **`server.js`** | JavaScript | Static files + API proxy | Simple, fast, reliable |

### Key Features

- 🎯 **Fast rendering**: Client-side rendering and routing
- 🔧 **API proxy support**: Optional backend proxying for development
- 🐳 **Docker ready**: Containerized deployment
- 🌍 **Environment-aware**: Different configs for dev/deploy

## 🛠️ Development

### Available Scripts

#### Development

- **Usage**: `npm start`, `npm run start:with-proxy`
- **Purpose**: Local development server with hot reload
- **Proxy**: Optional API proxy to backend (when `start:with-proxy`)

#### Building

```powershell
npm run build              # Deployment build
npm run build:dev          # Development build  
npm run watch              # Development build with file watching
```

#### Serving Built App

```powershell
npm run serve              # Serve built application
```

## 🔧 Configuration

### Environment Files

- **environment.ts**: Local development settings
- **environment.deploy.ts**: Deployment/Docker settings

### Environment Variables

| Variable | Values | Default | Description |
|----------|--------|---------|-------------|
| `PORT` | Number | `4000` | Server port |
| `BACKEND_SERVICE_URL` | URL | `http://backend:5100` | Backend API URL |
| `ENABLE_API_PROXY` | `true`, `false` | `false` | Enable API proxying |

## 📚 Usage Scenarios

### Docker Deployment

```powershell
# Build for deployment
docker build -t grants-frontend .

# Run with custom backend URL
docker run -e BACKEND_SERVICE_URL=https://api.example.com -p 4000:4000 grants-frontend
```

## 🏗️ Build Process

| **Scenario** | **Command** | **Output** | **Notes** |
|-------------|-------------|------------|-----------|
| **Local Dev** | `npm start` | Dev server | Hot reload, source maps |
| **Deployment Build** | `npm run build` | `dist/frontend/browser` | Optimized, minified |
| **Docker** | `docker build` | Container | Deployment build + server |

## 🐳 Docker

### Docker Build

```powershell
docker build -t grants-frontend .
```

### Docker Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `3000` | Application server port |
| `BACKEND_SERVICE_URL` | `http://backend:5100` | Backend API URL for Docker environments |
| `ENABLE_API_PROXY` | `false` | Enable API proxy for local Docker testing |

### API Connection Strategies

1. **Local Development**: Direct connection to `https://localhost:7000`
2. **Docker Deployment**: Uses `/api` with OpenShift routing to backend
3. **Local Docker Testing**: Enable proxy with `ENABLE_API_PROXY=true`

### Docker Compose

```powershell
# Build and run with default settings
docker-compose up --build frontend

# With API proxy for local testing
$env:ENABLE_API_PROXY="true"; docker-compose up --build frontend

# Full stack
docker-compose up --build
```

## 🎯 Deployment

### Local Testing

```powershell
npm start                  # Development server with hot reload
npm run start:with-proxy   # With API proxy to backend
npm run serve              # Serve built application
```

### OpenShift Deployment

```yaml
# Example deployment configuration
apiVersion: apps/v1
kind: Deployment
metadata:
  name: grants-frontend
spec:
  template:
    spec:
      containers:
      - name: frontend
        image: grants-frontend:latest
        env:
        - name: PORT
          value: "3000"
        # OpenShift handles /api routing to backend service
```

## 📁 Project Structure

```text
src/
├── app/                    # Angular application
├── environments/          # Environment configurations
└── styles/                # Global styles

server.js               # Application server (JavaScript)
package.json               # Dependencies and scripts
angular.json               # Angular CLI configuration
Dockerfile                 # Docker build configuration
```

## 🔍 Troubleshooting

### Port Issues

- Default port: 4000
- Change with: `PORT=3000 npm run serve`

### API Connection

- Check `BACKEND_SERVICE_URL` environment variable
- Verify backend is running and accessible
- Use `ENABLE_API_PROXY=true` for development

### Build Issues

- Clear cache: `ng cache clean`
- Fresh install: `rm -rf node_modules && npm install`
