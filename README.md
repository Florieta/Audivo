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
npm run dev
```

The app runs at `http://localhost:5173`.

## Project Structure

```
Audivo/
├── client/                         # React frontend
│   └── src/
│       ├── app/                    # Redux store & typed hooks
│       ├── components/             # Shared UI (Layout, Navbar, ProtectedRoute)
│       ├── features/
│       │   ├── auth/               # Auth slice, Login/Register pages
│       │   └── audiobooks/         # Audiobooks page (placeholder)
│       ├── services/               # API client & service layer
│       ├── theme/                  # MUI theme
│       └── types/                  # Shared TypeScript interfaces
├── server/                         # .NET backend
│   ├── src/
│   │   ├── Audivo.Core/            # Domain entities (no dependencies)
│   │   ├── Audivo.Application/     # Use cases, DTOs, interfaces
│   │   ├── Audivo.Infrastructure/  # EF Core, Identity, services
│   │   └── Audivo.API/             # Controllers, middleware, startup
│   └── Audivo.sln
└── README.md
```

## Architecture

**Clean Architecture** with strict dependency direction:

```
API → Application → Core ← Infrastructure
```

- **Core**: Domain entities, no framework dependencies
- **Application**: Use cases, DTOs, service interfaces
- **Infrastructure**: EF Core, Identity, JWT token generation
- **API**: Thin controllers, global error handling, API versioning (`/api/v1/`)

## Development Standards

- **Frontend**: ESLint + Prettier, TypeScript strict mode, no `any`
- **Backend**: Nullable reference types, Roslyn analyzers, `TreatWarningsAsErrors`, `dotnet format`
