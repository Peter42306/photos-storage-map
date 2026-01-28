# Photos Storage Map

TODO

Features:
- 

## Screenshots

## Technology stack

**Backend**

- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- RESTful API design
- File upload & storage (local filesystem)

 **Frontend**

- React (Vite)
- JavaScript
- Bootstrap 5

**Infrastructure**

- Docker
- docker-compose
- Nginx (reverse proxy)
- Linux server deployment 

## Project structure
```text
TODO
photos-storage-map/
│
├── backend/
│   └── PhotosStorageMap/
│       └── PhotosStorageMap/
│           ├── Program.cs
│           └── ...
│
├── frontend/
│   └── photos-storage-map-ui/
│       ├── src/
│       ├── package.json
│       └── vite.config.ts
│
└── README.md
```

## Environment variables

TODO

This project uses environment variables loaded from `.env` (local / server) and `.env.example` (template).

Create your own `.env` file based on `.env.example`.

### Required variables

#### PostgreSQL
- `POSTGRES_DB` — database name (e.g. `photosdb`)
- `POSTGRES_USER` — database user (e.g. `appuser`)
- `POSTGRES_PASSWORD` — database password

#### Ports (host → container)
- `POSTGRES_HOST_PORT` — local port for PostgreSQL (mapped to container `5432`)
- `API_HOST_PORT` — local port for backend API (mapped to container `8080`)
- `FRONTEND_HOST_PORT` — local port for frontend (mapped to container `80`)

### Notes
- Do **not** commit `.env` to GitHub. Store only `.env.example`.
- In production, uploads are stored on the server filesystem (e.g. `/var/www/uploads/photos-storage-geo`).
- In development, uploads are stored locally (see `docker-compose.dev.yml`).

## File storage

Uploaded files are stored outside containers on the host filesystem:

```text
TODO
/var/www/uploads/photos-storage-map/
    └── ???/
        └── {???Id}/            
```

The directory is mounted in the API container:

```text
volumes:
  - /var/www/uploads/?????
```

## API features

- TODO

## How to run

The project uses Docker and Docker Compose for both development and production environments.

### Development (local machine)

Used for local development on Windows / macOS / Linux.

Requirements:
- Docker
- Docker Compose

Steps:
1. Create a `.env` file based on `.env.example` and adjust ports if needed.
2. Make sure the uploads directory exists:

```text
photos-storage-geo/_data/uploads
```

3. Run the project:
```bash
docker compose -f docker-compose.dev.yml up -d --build
```

Services will be available at:

Frontend: http://localhost:<FRONTEND_HOST_PORT>
Backend API: http://localhost:<API_HOST_PORT>

To stop containers:

```text
docker compose -f docker-compose.dev.yml down
```

Uploaded files are stored locally in:
```text
photos-storage-geo/_data/uploads
```


### Production (server)

Used for deployment on a Linux server with Nginx as a reverse proxy.

Requirements:

Docker

Docker Compose

Nginx (already configured on the host)

Steps:

Create a .env file on the server (do not commit it to Git).

Make sure the uploads directory exists on the server:

```text
/var/www/uploads/photos-storage-geo
```

Run the project:
```text
docker compose up -d --build
```

Services run internally on the server:

Frontend  127.0.0.1:<FRONTEND_HOST_PORT>

Backend API  127.0.0.1:<API_HOST_PORT>

PostgreSQL  127.0.0.1:<POSTGRES_HOST_PORT>

Public access is handled by Nginx:

/ -> frontend

/api -> backend API

Uploaded files are stored on the server filesystem and persist across container restarts.



## Docker setup
The backend and database run in containers, frontend is served as static files.
```text
docker-compose up -d --build
```

Containers:
TODO
- 
