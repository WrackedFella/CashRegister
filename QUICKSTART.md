# Quick Start Commands

## Run with Docker Compose
docker-compose up --build

## Stop Services
docker-compose down

## View Logs
docker-compose logs -f

## Rebuild Everything
docker-compose down -v
docker-compose up --build --force-recreate

## Run Locally (Development)

### Server
cd CashRegister.Server
dotnet run

### Client
cd cashregister.client
npm install
npm run dev

## Access URLs
- Client: http://localhost:3000 (Docker) or http://localhost:5173 (Local Dev)
- Server: http://localhost:5000
