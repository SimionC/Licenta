# Copilot instructions for this repository

## Big picture architecture
- This is a two-project solution: ASP.NET Core backend in `App.Server/` and React + Vite frontend in `app.client/` (`App.sln`).
- Backend serves API controllers under `/api/*` and also hosts SPA static files (`App.Server/Program.cs` with `UseStaticFiles` + `MapFallbackToFile("/index.html")`).
- Frontend talks only to relative `/api/...` endpoints; Vite proxies `/api` to ASP.NET (`app.client/vite.config.js`).
- Persistence is EF Core + SQLite via `AppDbContext` (`App.Server/ORM/AppDbContext.cs`) and migrations in `App.Server/Migrations/`.
- Main functional areas are auth, courses, notes, and collaborations (`App.Server/Controllers/*Controller.cs`).

## Runtime and developer workflows
- Preferred local run from solution root:
  - Backend: `dotnet run --project App.Server/App.Server.csproj`
  - Frontend (separate terminal): `cd app.client && npm run dev`
- The server project is SPA-proxy configured (`SpaProxyLaunchCommand=npm run dev`), so Visual Studio can launch frontend from backend.
- Frontend dev server is configured to port `7167` (`app.client/vite.config.js`).
- DB connection used by server startup is `ConnectionStrings:DefaultConnection` (`App.Server/appsettings.json`), currently `Data Source=db/app.db`.
- EF commands are expected from `App.Server/` context:
  - `dotnet ef migrations add <Name>`
  - `dotnet ef database update`

## Auth and API integration conventions
- Auth is cookie-based (`AddAuthentication().AddCookie()` in `Program.cs`); client requests that need session must use credentials (`credentials: 'include'` or `withCredentials: true`).
- Auth endpoints are action-routed: `/api/Auth/Login`, `/api/Auth/Register`, `/api/Auth/Me` (`AuthController`).
- Most notes/collaboration endpoints require `[Authorize]` (`NotesController`, `CollaborationsController`).
- Claims are custom (e.g., `userId`, `Email`, `UserTypeId`) and read directly in controllers; preserve these names when modifying auth flow.

## Project-specific coding patterns
- Notes API maps DB `Note.Text` to DTO `NoteModel.Content`; preserve this translation pattern in queries and updates (`NotesController`).
- Collaboration note creation is two-step on client: create collaboration first, then create note with `collaborationId` (`CreateCollaborationPage.jsx`).
- Route shape matters for note editor context:
  - personal note: `/notes/:noteGuid`
  - collaboration note: `/collaborations/:collabId/notes/:noteGuid`
- Role checks are string-based (`owner`, `editor`) in collaboration membership logic.

## Known repo realities to respect
- `app.client/README.md` is default Vite template and not authoritative for this app.
- Markdown rendering deps (`react-markdown`, `remark-*`, `rehype-*`, `katex`, `highlight.js`) are present in root `package.json`; do not assume only `app.client/package.json` contains all frontend runtime deps.
- There are no configured GitHub workflow files under `.github/workflows/` at the moment.
- Keep changes focused; follow existing mixed fetch/axios style in touched files instead of broad refactors unless explicitly requested.