# Grants Applicant Portal Solution

A full-stack application for managing grant applications, built with Angular frontend and .NET backend.

## 🏗️ Architecture

### Stack Components

| Component | Technology | Port | Description |
|-----------|------------|------|-------------|
| **Frontend** | Angular 20 SPA | 4200 | User interface and application logic |
| **Backend** | .NET 9 Web API | 5100 | REST API and business logic |
| **Database** | PostgreSQL 17 | 5434 | Primary data storage |
| **Cache** | Redis 7 | 6379 | Session and caching layer |
| **Admin UI** | Redis Commander | 8081 | Redis management interface |

### API Routing Strategies

The application uses different API routing strategies depending on the deployment environment:

1. **Local Development**: Frontend connects directly to `https://localhost:7000` (backend dev server)
2. **Local Docker**: Frontend uses reverse proxy (`ENABLE_API_PROXY=true`) to route `/api` → `backend:5100`
3. **OpenShift Deployment**: Platform handles `/api` routing (proxy disabled by default in Dockerfile)

## 🚀 Quick Start

### Full Stack (Recommended)

```powershell
# Clone and navigate to repository
git clone <repository-url>
cd Grants/applications/Grants.ApplicantPortal

# Start complete stack
docker-compose up --build

# Access applications:
# Frontend: http://localhost:4200
# Backend API: http://localhost:5100
# Redis Commander: http://localhost:8081
```

### Individual Services

```powershell
# Frontend only
docker-compose up --build frontend

# Backend with dependencies
docker-compose up --build backend postgres redis

# Database only
docker-compose up postgres
```

## 🔧 Configuration

### Environment Variables

#### Frontend Service

| Variable | Default | Description |
|----------|---------|-------------|
| `PORT` | `4200` | Frontend server port |
| `ENABLE_API_PROXY` | `true` | Enable reverse proxy for Docker deployment |
| `BACKEND_SERVICE_URL` | `http://backend:5100` | Backend service URL for Docker networking |

#### Backend Service

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | .NET environment configuration |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | Database connection string |
| `ConnectionStrings__Redis` | `redis:6379` | Redis connection string |

### Development vs Deployment

#### Local Development (Direct Connection)

- **Frontend**: `npm start` → connects to `https://localhost:7000`
- **Backend**: `dotnet run` → runs on `https://localhost:7000`
- **Database**: Local PostgreSQL or Docker container
- **API Proxy**: Disabled (direct connection)

#### Local Docker Deployment (Reverse Proxy)

- **Frontend**: Docker container with `ENABLE_API_PROXY=true`
- **Backend**: Docker container on internal network
- **Database**: PostgreSQL container
- **API Routing**: Frontend proxy routes `/api` → `backend:5100`

#### OpenShift Deployment (Platform Routing)

- **Frontend**: Container with `ENABLE_API_PROXY=false` (default)
- **Backend**: Container on platform network
- **Database**: Platform-managed PostgreSQL
- **API Routing**: OpenShift routes handle `/api` → backend service

## 📁 Project Structure

```text
src/
├── Grants.ApplicantPortal.Backend/    # .NET Web API
│   ├── Controllers/                   # API endpoints
│   ├── Services/                      # Business logic
│   ├── Models/                        # Data models
│   └── Dockerfile                     # Backend container
├── Grants.ApplicantPortal.Frontend/   # Angular SPA
│   ├── src/app/                       # Angular application
│   ├── src/environments/              # Environment configs
│   ├── server.js                      # Node.js server
│   └── Dockerfile                     # Frontend container
docker-compose.yml                     # Stack orchestration
```

## Docker Compose Services
### Production-Ready Stack

```yaml
services:
  frontend:    # Angular SPA with Node.js server
  backend:     # .NET Web API
  postgres:    # PostgreSQL database
  redis:       # Redis cache
  redis-commander: # Redis management UI
```

### Service Dependencies

```text
frontend → backend → postgres
        → redis
redis-commander → redis
```

## 🎯 Deployment Scenarios

### Local Testing

```powershell
docker-compose up --build
```

### Development with Hot Reload

```powershell
# Backend only (for frontend development)
docker-compose up postgres redis backend

# Frontend development server
cd src/Grants.ApplicantPortal.Frontend
npm start
```

### OpenShift Deployment

- Frontend: Container image with proxy disabled
- Backend: Container image with platform database
- Database: Platform-managed PostgreSQL service
- Routing: Platform handles `/api` → backend routing

## 🔍 Monitoring & Management

| Service | URL | Purpose |
|---------|-----|---------|
| Frontend | <http://localhost:4200> | Main application |
| Backend API | <http://localhost:5100> | API documentation/health |
| Redis Commander | <http://localhost:8081> | Cache management |
| PostgreSQL | localhost:5434 | Database access |

## 🛠️ Development

See individual service READMEs for detailed development instructions:

- [Frontend Development](src/Grants.ApplicantPortal.Frontend/README.md)
- [Backend Development](src/Grants.ApplicantPortal.Backend/README.md)

## 📋 Prerequisites

- Docker & Docker Compose
- Node.js 20+ (for local frontend development)
- .NET 9 SDK (for local backend development)
- PowerShell or Bash
