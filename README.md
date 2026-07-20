# PhotoMap

***PhotoMap*** is a web application for storing, organizing and exploring photo collections.

Designed to create collections, upload photos, add notes, view them on an interactive map, generate archives, and securely share collections with others.

## Features
- Photo collections
- Interactive map
- Notes
- ZIP archives
- Secure sharing
- Object Storage (S3 API)
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
- S3 API (Hetzner Object Storage)
- ImageSharp
- SendGrid
- Background Services

### Frontend

- React
- Vite
- React Router
- Bootstrap 5
- Bootstrap Icons
- React Bootstrap
- MapTiler

### Infrastructure

- Ubuntu
- Nginx
- systemd
- Git

## File storage

PhotoMap uses Hetzner Object Storage (S3 API).

- Original photos, thumbnails, resized images and ZIP archives are stored in Object Storage.
- Presigned URLs are used for direct browser uploads.
- Background workers clean up temporary and deleted files.
- Private objects are protected with time-limited access.

## Backups

- Automated PostgreSQL backups using `pg_dump`
- Daily database backups to Hetzner Storage Box
- Automatic backup retention, 7 days

## API Features

- User registration and authentication
- Google Sign-In
- Email confirmation and password recovery
- Photo collection management
- Direct S3 uploads using presigned URLs
- Background image processing
- Thumbnail and resized image generation
- ZIP archive support
- Interactive map with geotagged photos
- Secure collection sharing
- Collection statistics
- Automatic cleanup workers

## Background Workers

- Asynchronous photo processing
- Uploading processed images to S3 Object Storage
- Thumbnail generation
- Resized image generation
- Original photo cleanup
- Individual photo deletion
- Collection cleanup

## Administration

The application includes an administration panel for managing users and monitoring system usage. 

Features:

- User management
- Enable or disable Pro Storage plans
- Activate or deactivate user accounts
- Collection, photo and archive statistics
- Storage usage monitoring
- Last login tracking
- Registration date tracking
- System-wide totals
 
## Status

The application is deployed and fully operational. New features and improvements are planned for future releases.
