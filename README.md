# Audivo

A modern audiobook application that allows users to listen to books anytime, anywhere in a simple and enjoyable way.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | React 19, TypeScript (strict), Redux Toolkit, Material UI 7, Vite |
| **Backend** | .NET 10, ASP.NET Core Web API, Entity Framework Core |
| **Database** | SQL Server (LocalDB for development) |
| **Auth** | ASP.NET Identity + JWT Bearer (access token in memory, refresh token in httpOnly cookie) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview)
- [Node.js 20+](https://nodejs.org/) with npm
- SQL Server LocalDB (included with Visual Studio)

## Getting Started

### Backend

```bash
cd server
dotnet restore
dotnet ef database update --project src/Audivo.Infrastructure --startup-project src/Audivo.API
dotnet run --project src/Audivo.API
```

The API runs at `https://localhost:7196` (or the port shown in console).  
Swagger UI: `https://localhost:7196/swagger`

### Frontend

```bash
cd client
npm install
# copy .env.example to .env and keep the default API URL for local dev
npm run dev
```

The app runs at `http://localhost:5173`.

## About Audivo

Audivo is an audiobook platform for discovering, organizing, and listening to audiobooks in one place. It combines a modern web interface with a secure API and persistent user data so listeners can continue seamlessly across sessions.

The application is designed around a simple experience: sign in, browse the catalog, start listening, and pick up from where you left off.

## Core Functionalities

- **Authentication & session management**
	- User registration and login
	- JWT-based authentication with refresh token cookie flow
	- Protected routes and secure API access

- **Gallery & discovery**
	- Browse the full audiobook catalog
	- Real-time search by title, author, genre, and description
	- Filter by genre and author
	- Sort by recently added, title, or author

- **Audiobook playback experience**
	- Open any title in the player and start listening immediately
	- Continue listening from the last known position
	- Track progress and completion state

- **Library management**
	- View uploaded audiobooks
	- Mark and unmark favorites
	- Access favorite books in a dedicated view

- **Profile management**
	- View personal profile information
	- Update profile details and profile image

- **Platform quality features**
	- API versioning and centralized exception handling
	- Validation across backend and frontend flows
	- Structured, maintainable full-stack setup for continued feature growth
