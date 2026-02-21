# Audivo Client

React frontend for the Audivo audiobook platform.

## Tech Stack

- React 19
- TypeScript (strict)
- Redux Toolkit
- Material UI 7
- Vite

## Prerequisites

- Node.js 20+
- npm
- Audivo API running locally (default: `https://localhost:7196`)

## Environment Setup

Create a local environment file from the committed example:

```bash
cp .env.example .env
```

For Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

Default value:

```env
VITE_API_URL=https://localhost:7196
```

## Run Locally

```bash
npm install
npm run dev
```

The app runs at `http://localhost:5173`.

## Build

```bash
npm run build
```

## Notes

- Authentication uses an in-memory access token and HttpOnly refresh-token cookie flow.
- Ensure backend CORS includes your frontend origin for local development.
