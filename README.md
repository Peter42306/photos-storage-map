# PhotoMap

***PhotoMap*** is a web application for storing, organizing and exploring photo collections.

Designed to create collections, upload photos, add notes, view them on an interactive map, generate archives, and securely share collections with others.

## Features
- Photo collections
- Interactive map
- Notes
- ZIP archives
- Secure sharing
- Object Storage (S3)
- JWT Authentication
- Google Sign-In
- Collection statistics
- Background cleanup workers

## Screenshots

<img width="1920" height="1080" alt="Screenshot-2026-06-29 151055-map-page" src="https://github.com/user-attachments/assets/51fa8cea-f2eb-4ab3-94f7-de3a291eaecc" />

<img width="1904" height="1071" alt="Screenshot-2026-06-29-150857-collection-page" src="https://github.com/user-attachments/assets/564630aa-a7d8-4b98-a29d-4eeb961a3ebf" />

<img width="1904" height="1071" alt="Screenshot-2026-06-29-150758-my-collections" src="https://github.com/user-attachments/assets/90dd9959-8e0d-4d4e-ac22-cbcf00d3e46f" />

## Project Structure
```text
photos-storage-map/
├── backend/
│   └── PhotosStorageMap/
│       ├── Api/
│       │   ├── Authorization/
│       │   ├── Controllers/
│       │   ├── Extensions/
│       │   ├── Services/
│       │   └── CorsPolicies.cs
│       │
│       ├── Application/
│       │   ├── Common/
│       │   ├── DTOs/
│       │   ├── Images/
│       │   └── Interfaces/
│       │
│       ├── Domain/
│       │   ├── Entities/
│       │   ├── Enums/
│       │   └── ValueObjects/
│       │
│       ├── Infrastructure/
│       │   ├── BackgroundProcessing/
│       │   ├── Data/
│       │   ├── Email/
│       │   ├── Extensions/
│       │   ├── Identity/
│       │   ├── Images/
│       │   ├── Policies/
│       │   ├── Services/
│       │   └── Storage/
│       │
│       ├── Program.cs
│       └── appsettings.json
│
├── frontend/
│   ├── public/
│   │   └── images/
│   │       └── landing/
│   │
│   ├── src/
│   │   ├── assets/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── routes/
│   │   ├── api.js
│   │   ├── contactFormApi.js
│   │   ├── App.jsx
│   │   └── main.jsx
│   │
│   ├── package.json
│   └── vite.config.js
│
├── .github/
├── .gitignore
└── README.md
```

## Technology stack

### Backend

- C#
- ASP.NET Core 8
- Entity Framework Core
- ASP.NET Core Identity
- JWT Authentication
- Google OAuth 2.0
- PostgreSQL
- Amazon S3 API (Hetzner Object Storage)
- ImageSharp
- SendGrid
- Background Services (`IHostedService`)

### Frontend

- React
- Vite
- React Router
- Bootstrap 5
- Bootstrap Icons
- React Bootstrap
- Leaflet

### Infrastructure

- Ubuntu
- Nginx
- systemd
- Git

## File storage

PhotoMap stores uploaded files in Hetzner Object Storage using the Amazon S3 API.

- Original photos stored in object storage
- Presigned URLs for direct browser uploads
- Automatic thumbnail and resized image generation
- ZIP archive storage
- Background cleanup of temporary and deleted files
- Secure private storage with time-limited access

## Backups

- The application uses automated PostgreSQL backups.
- Database backups are created with `pg_dump`
- Backup files are stored in Hetzner Storage Box
- Object Storage and database are separated

## API Features

- User registration and authentication
- Google Sign-In
- Email confirmation and password recovery
- Photo collection management
- Direct S3 uploads using presigned URLs
- Background image processing
- Thumbnail and resized image generation
- ZIP archive support
- Interactive map with GPS coordinates
- Secure collection sharing
- Collection statistics
- Automatic cleanup workers

## Background Workers

- Photo processing
- Original photo cleanup
- Collection cleanup
- Archive generation

## Status

The application is deployed and fully operational. New features and improvements are planned for future releases.
