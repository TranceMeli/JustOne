# JustOne

![.NET](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-5C2D91?style=for-the-badge&logo=dotnet&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQLite](https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Vue.js](https://img.shields.io/badge/Vue.js-4FC08D?style=for-the-badge&logo=vuedotjs&logoColor=white)
![Vite](https://img.shields.io/badge/Vite-646CFF?style=for-the-badge&logo=vite&logoColor=white)
![Pinia](https://img.shields.io/badge/Pinia-FFD859?style=for-the-badge&logo=vue.js&logoColor=black)
![Axios](https://img.shields.io/badge/Axios-5A29E4?style=for-the-badge&logo=axios&logoColor=white)

A social network with one simple rule: **one photo per day, nothing more.** No endless feed, no pressure to post constantly, just a daily snapshot collected in a monthly overview.

## Screenshot

![JustOne calendar view](docs/screenshot.jpeg)

![JustOne comments view](docs/comments-screenshot.jpeg)

## Concept

Every user sees their month as a grid of boxes, one per day. If a photo was posted, the box shows it. If not, it's marked empty. Posting is only possible for the **current day**. There's no posting for past dates.

## Features

- **Auth with JWT + refresh tokens**: short-lived access token, refresh token with rotation and theft detection (a reused, already-invalidated token automatically revokes all active sessions for that account)
- **Role system**: `Admin`, `Manager`, `User`; Manager and Admin can moderate other users' posts (replace the image or delete it)
- **Self-registration**: new accounts always start as `User`; role upgrades only by an admin
- **Monthly calendar**: dynamic day count per month, navigation from January to the current month
- **One post per day**: enforced server-side (not just in the frontend), including change/delete for today's post
- **Daily comments**: comments attached directly to a post, with author display
- **Guestbook**: a dedicated section below the calendar
- **Emoji support** in comments

## Tech Stack

**Backend:** ASP.NET Core 10 (controller-based Web API), Entity Framework Core with SQLite, ASP.NET Core Identity, JWT bearer auth with refresh token rotation, Swagger/Swashbuckle

**Frontend:** Vue 3 (Composition API), Vite, Pinia, Axios

## Project Structure

```
JustOne/
├── asp-backend/               # ASP.NET Core backend
│   ├── Controller/            # Auth, Posts, Comments, Guestbook
│   ├── Entities/              # Post, Comment, RefreshToken, GuestbookEntry, ...
│   ├── Models/                 # Request/response DTOs
│   ├── Data/                   # AppDbContext, DbSeeder
│   └── Auth/                   # JWT settings, TokenService
└── vue-frontend/               # Vue 3 frontend
    └── src/
        ├── components/         # Login, Register, PostCalendar, PostUpload, Comments, Guestbook, ImageModal
        ├── composables/         # usePostCalendar
        ├── stores/              # auth (Pinia)
        └── api/                 # Axios client with auth interceptor
```

## Setup

### Backend

```bash
cd asp-backend
dotnet restore
dotnet ef database update
dotnet run
```

Swagger UI is then available at `https://localhost:7164/swagger`.

### Frontend

```bash
cd vue-frontend
npm install
npm run dev
```

The app runs at `http://localhost:5173`.

## Status

Actively in development, a portfolio project built for software developer job applications.