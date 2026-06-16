# Todo App

A full-stack to-do task manager built with **.NET 8 Web API**, **React + TypeScript**, **SQLite** (dev/tests), and **PostgreSQL** (production/Docker).

---

## Project Structure

```
simple-todo/
├── backend/
│   ├── TodoApp.API/             # ASP.NET Core Web API
│   │   ├── Controllers/         # Thin HTTP layer
│   │   ├── Services/            # Business logic + mapping
│   │   ├── Repositories/        # Data access (EF Core)
│   │   ├── Models/              # Domain entities
│   │   ├── DTOs/                # Request/response contracts
│   │   ├── Data/                # DbContext + design-time factory
│   │   ├── Middleware/          # Global error handling
│   │   └── Migrations/          # EF Core migrations (generated against Postgres)
│   ├── TodoApp.Tests/           # xUnit tests (services + repositories)
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── api/                 # Axios API client
│   │   ├── components/          # React components
│   │   ├── hooks/               # useTodos custom hook
│   │   ├── types/               # TypeScript interfaces
│   │   └── test/                # Vitest + React Testing Library
│   ├── nginx.conf               # Production nginx config (API proxy + SPA fallback)
│   └── Dockerfile
├── docker-compose.yml           # Production-style compose
├── docker-compose.override.yml  # Dev overrides (exposes backend port)
└── README.md
```

---

## Quick Start

### Option 1 — Docker Compose (recommended)

Requires Docker Desktop or Docker Engine + Compose v2.

```bash
# Build and start all three services (db, backend, frontend)
docker compose up --build

# App is available at:
#   http://localhost:3000        -> React frontend
#   http://localhost:5001/swagger -> Swagger UI (dev override exposes this)
```

To stop:
```bash
docker compose down
# To also remove the PostgreSQL volume:
docker compose down -v
```

> **Important:** always use `docker compose down -v` when switching between
> incompatible schema versions during development. This drops the Postgres
> volume so EF migrations run from scratch on the next start.

---

### Option 2 — Local development (no Docker)

**Prerequisites:** .NET 8 SDK, Node.js 20+

#### Backend

```bash
cd backend
dotnet restore
dotnet run --project TodoApp.API
# API runs at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

The backend defaults to SQLite (`todo_dev.db`) in Development mode.

#### Run backend tests

```bash
cd backend
dotnet test
```

Tests use SQLite in-memory — no external dependencies needed.

#### Adding or modifying EF Core migrations

Migrations must be generated using the Postgres provider (via `TodoDbContextFactory`)
so column types are correct for both Postgres and SQLite:

```bash
cd backend
dotnet ef migrations add <MigrationName> --project TodoApp.API/TodoApp.API.csproj
```

The `IDesignTimeDbContextFactory` in `Data/TodoDbContextFactory.cs` forces the
Postgres provider at design time regardless of the local `appsettings.json` setting.
Never edit migration files by hand — always regenerate.

#### Frontend

```bash
cd frontend
npm install
npm run dev
# React app at http://localhost:3000
# Vite proxies /api/* to http://localhost:5000
```

#### Run frontend tests

```bash
cd frontend
npm test
```

---

## API Reference

| Method | Endpoint         | Description                      |
|--------|------------------|----------------------------------|
| GET    | `/api/todo`      | List tasks (filterable, paged)   |
| GET    | `/api/todo/{id}` | Get single task                  |
| POST   | `/api/todo`      | Create task                      |
| PUT    | `/api/todo/{id}` | Partial update task              |
| DELETE | `/api/todo/{id}` | Delete task                      |

### Query parameters for `GET /api/todo`

| Param         | Type    | Default     | Description                              |
|---------------|---------|-------------|------------------------------------------|
| `isCompleted` | bool    | —           | Filter by completion status              |
| `priority`    | string  | —           | Filter by priority: `Low`, `Medium`, `High` |
| `search`      | string  | —           | Full-text search on title and description |
| `page`        | int     | `1`         | Page number (1-based)                    |
| `pageSize`    | int     | `20`        | Items per page (max 100)                 |
| `sortBy`      | string  | `dueDate`   | Sort field: `dueDate`, `createdAt`, `priority`, `title` |
| `sortDir`     | string  | `asc`       | Sort direction: `asc`, `desc`            |

Default sort is **due date ascending** — tasks due soonest appear first (today before tomorrow); tasks without a due date are pushed to the bottom regardless of direction.

### Error responses

All unhandled server errors return HTTP 500 with the following shape:

```json
{
  "errorId": "ERR-3A9F1C2B",
  "message": "An unexpected error occurred."
}
```

The `errorId` is a short opaque token. The full error details (exception, stack
trace, request path) are logged server-side under the same token. Users should
quote the `errorId` when reporting issues; operators can grep logs for it directly.

---

## Architecture & Design Decisions

### Backend — Clean layered architecture

```
HTTP Request
  -> ErrorHandlingMiddleware  (outermost — catches all unhandled exceptions)
  -> Controller               (parse input, return HTTP responses — no logic)
  -> Service                  (business rules, validation, DTO mapping)
  -> Repository               (data access via EF Core)
  -> Database
```

**Why this separation?**
- Controllers stay thin and testable — no SQL or business logic.
- Services are unit-tested with a mocked repository (fast, no I/O).
- Repositories are tested with SQLite in-memory for full SQL fidelity.
- Swapping PostgreSQL for another DB only touches the repository + `Program.cs`.

### Error handling

`ErrorHandlingMiddleware` is registered as the first middleware so it wraps the
entire pipeline. Every unhandled exception gets a generated `ERR-XXXXXXXX` token
that is logged with the full exception and returned to the client as the only
error detail. No stack traces, SQL messages, or file paths ever reach the browser.

### Database provider switching

`appsettings.json` has a `DbProvider` key. Set it to `"postgres"` and provide a
`ConnectionStrings:Postgres` value to use PostgreSQL. Leave it as `"sqlite"` for
local development. No code changes required.

Migrations are always generated with the Postgres provider via
`TodoDbContextFactory` (`IDesignTimeDbContextFactory`), which ensures column types
(`boolean`, `integer`, `character varying`, `timestamp without time zone`) are
correct for Postgres. EF's provider-specific type resolution handles SQLite
automatically at runtime.

### Partial updates

`PUT /api/todo/{id}` accepts a `UpdateTodoItemRequest` with all nullable fields.
The service only applies fields that are non-null, making it a practical
partial-update without needing PATCH/JSON Patch. Trade-off: you cannot explicitly
clear a field to `null` with this approach (acceptable for this scope).

### Pagination & sorting

The list endpoint is paginated (default page size 20, max 100). Sorting is
controlled by `sortBy` and `sortDir` query parameters. The default sort — due date
ascending, nulls last — surfaces the most time-sensitive tasks first (today before
tomorrow). All sort fields support both directions; null due dates are always placed
at the bottom regardless of direction.

### Frontend — Component hierarchy

```
App (owns useTodos hook state)
  ├── TodoForm       (controlled form, calls createTodo)
  ├── TodoFilterBar  (filter controls, calls updateFilters)
  ├── TodoList
  │     └── TodoItem   (inline edit, toggle, delete)
  └── Pagination     (page navigation, hidden when totalPages <= 1)
```

No global state library (Redux/Zustand) is used. The `useTodos` hook owns all
state — items, totalCount, currentPage, filters — and exposes `updateFilters`
(resets to page 1) and `goToPage` (page-only change) as separate callbacks.

### nginx as reverse proxy

In Docker, the frontend nginx container proxies `/api/*` to the backend. This means:
1. The browser only talks to one origin -> no CORS in production.
2. The backend container is not publicly exposed.
3. Static files are served efficiently from nginx's cache.

---

## Trade-offs & Assumptions

| Decision | Rationale | Alternative |
|----------|-----------|-------------|
| Manual DTO mapping in service | Keeps dependencies minimal | AutoMapper at larger scale |
| Enums stored as integers | Efficient; avoids schema migration on rename | Store as strings for SQL readability |
| `PUT` for partial updates (all-nullable fields) | Simple client API | JSON Patch (`PATCH`) for strict semantics |
| No auth/authorization | Out of scope; see Future Work | JWT + ASP.NET Identity |
| Auto-migrate on startup | Convenient for small apps | Dedicated migration job in CI/CD |
| Invalid priority defaults to Medium | Forgiving for UI-driven input | Return 400 for strict validation |
| No debounce on search filter | Keeps code simple; one request per keystroke | `useDebounce` hook (e.g., 300 ms) |
| `window.confirm` for delete | Zero-dependency confirmation | Custom modal component |
| Migrations generated against Postgres | Ensures correct column types; SQLite handled at runtime | Separate migration sets per provider |
| Opaque error IDs to clients | Prevents internal detail leakage; correlates to server logs | Structured ProblemDetails with safe fields only |
| Nulls-last for due date sort | Tasks without a date shouldn't crowd out time-sensitive ones | Configurable null placement |

---

## Future Work

Given more time, the next priorities would be:

1. **Authentication & Authorization** — JWT-based auth with ASP.NET Identity; user-scoped tasks.
2. **Soft delete** — Add `DeletedAt` column; filter out in queries; expose a "restore" endpoint.
3. **Tags / labels** — Many-to-many relationship between tasks and tags.
4. **Subtasks** — Self-referential foreign key on `TodoItem`.
5. **Real-time updates** — SignalR WebSocket for multi-tab/multi-user sync.
6. **Optimistic UI + React Query** — Replace the manual `useTodos` hook with React Query for caching, background refetch, and optimistic updates.
7. **Search debouncing** — Avoid per-keystroke API calls in the filter bar.
8. **Integration / E2E tests** — Playwright or Cypress for full-stack smoke tests.
9. **CI/CD pipeline** — GitHub Actions: run tests, build Docker images, push to registry.
10. **Observability** — Structured logging (Serilog), health-check endpoint, Prometheus metrics.
11. **DB migrations in CI** — Move `db.Database.MigrateAsync()` out of app startup into a dedicated init container or migration job.
12. **Structured error responses** — Adopt RFC 9457 `ProblemDetails` for validation errors alongside the current opaque-ID pattern for server errors.
