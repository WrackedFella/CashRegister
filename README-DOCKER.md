# Docker Setup Guide

This project uses Docker Compose to run the Cash Register application with separate client and server containers.

## Architecture

- **Client**: React + Vite app served via Nginx on port 3000
- **Server**: .NET 9 API on port 5000
- **Network**: Both containers communicate via a Docker bridge network

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Running the Application

### Using Docker Compose (Recommended)

```powershell
# Build and start both services
docker-compose up --build

# Or run in detached mode
docker-compose up -d --build
```

Access the application:
- **Client**: http://localhost:3000
- **Server API**: http://localhost:5000

### Stop the Application

```powershell
docker-compose down
```

### View Logs

```powershell
# All services
docker-compose logs

# Specific service
docker-compose logs client
docker-compose logs server

# Follow logs
docker-compose logs -f
```

## Development Setup

### Local Development (Without Docker)

#### Server
```powershell
cd CashRegister.Server
dotnet run
```
Server runs on http://localhost:5000

#### Client
```powershell
cd cashregister.client
npm install
npm run dev
```
Client runs on http://localhost:5173

The Vite dev server is configured to proxy API requests to the .NET server.

## Docker Configuration Details

### Client Container
- **Base Image**: nginx:alpine
- **Build**: Multi-stage build with Node.js 20
- **Port**: 3000 (mapped to 80 inside container)
- **Features**:
  - Nginx serves built React app
  - Proxies `/cashregister/*` requests to server
  - Client-side routing support

### Server Container
- **Base Image**: mcr.microsoft.com/dotnet/aspnet:9.0
- **Build**: Multi-stage build with .NET SDK 9.0
- **Port**: 5000 (mapped to 8080 inside container)
- **Features**:
  - CORS enabled for client access
  - Health check endpoint

## Troubleshooting

### Container not starting
```powershell
# Check container status
docker-compose ps

# Check container logs
docker-compose logs server
docker-compose logs client
```

### Network issues
```powershell
# Verify network
docker network ls
docker network inspect cashregister_cashregister-network
```

### Clean rebuild
```powershell
# Remove containers, networks, and volumes
docker-compose down -v

# Remove images
docker-compose down --rmi all

# Rebuild from scratch
docker-compose up --build --force-recreate
```

### Can't connect to API
- Ensure both containers are running: `docker-compose ps`
- Check if server is healthy: `docker-compose logs server`
- Verify CORS configuration in `Program.cs`
- Check nginx proxy configuration in `nginx.conf`

## File Structure

```
CashRegister/
├── docker-compose.yml              # Orchestrates both services
├── CashRegister.Server/
│   ├── Dockerfile                  # Server container build
│   └── .dockerignore              # Server build exclusions
└── cashregister.client/
    ├── Dockerfile                  # Client container build
    ├── nginx.conf                  # Nginx configuration
    └── .dockerignore              # Client build exclusions
```

## Notes

- The client and server are fully independent containers
- Communication happens via Docker network (not localhost inside containers)
- Production builds are optimized and minified
- Nginx handles static file serving and API proxying in production
