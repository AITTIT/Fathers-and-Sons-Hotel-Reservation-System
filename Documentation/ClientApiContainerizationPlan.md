# MAUI + API + Docker Containerization Plan

## 1. Agreed Direction

This plan is based on the current decisions:

- Scope: **Backend skeleton first**
- Auth (initial): **No auth yet**
- Local container orchestration: **Docker Compose**
- Database persistence: **SQLite with named Docker volume**
- Shared contracts: **Shared class library**

---

## 2. Target Architecture

```text
MAUI Client (MVVM)
   |
   | HTTP (JSON)
   v
ASP.NET Core Web API (Docker container)
   |
   | EF Core provider / data layer
   v
SQLite database file (stored on Docker named volume)
```

### MVVM impact

MVVM remains valid and recommended:

- **Views + ViewModels stay in MAUI client**
- **Domain/application logic and persistence move to API**
- MAUI ViewModels call API services (abstractions), not direct DB/repository classes
- API becomes the boundary for validation, business rules, and data ownership

---

## 3. Solution/Project Changes

## 3.1 Add projects

1. Keep existing MAUI project as client application.
2. Add new **ASP.NET Core Web API** project (backend).
3. Add new **Shared.Contracts** class library for DTOs and request/response models.
4. Add new **Infrastructure/Data** backend layer (or folders) for EF Core + SQLite context/migrations.

## 3.2 Define boundaries

- MAUI project:
  - ViewModels
  - UI state
  - API client services
  - no direct EF/SQLite references
- API project:
  - Controllers/endpoints
  - Application services/use cases
  - Validation
  - Data access/repositories
- Shared.Contracts:
  - DTOs used by both MAUI and API
  - No UI-specific types
  - No persistence-specific entities

---

## 4. Containerization Changes

1. Add `Dockerfile` for API project.
2. Add `docker-compose.yml` with services:
   - `api`
   - `sqlite` volume mount for database file persistence
3. Add environment variables for API:
   - ASP.NET Core environment
   - Connection string path
4. Add `.dockerignore` for API project.
5. Add initialization strategy for database:
   - apply migrations at startup (development) or via explicit migration command.

---

## 5. Backend Skeleton (First Milestone)

1. Create minimal API structure:
   - Health endpoint (`/health`)
   - One sample feature endpoint (e.g., rooms list placeholder)
2. Configure EF Core with SQLite.
3. Add DbContext and initial migration.
4. Ensure API starts inside Docker and can access persistent SQLite file.
5. Verify MAUI client can call backend endpoint via configurable base URL.

---

## 6. MAUI Client Refactor Plan

1. Introduce API service abstractions in MAUI (per feature).
2. Replace direct local data access with HTTP-based implementations.
3. Keep ViewModels unchanged where possible except dependency swap.
4. Add environment-aware API base URL configuration for local/dev.
5. Add resilient error handling for API/network failures in ViewModels.

---

## 7. Tooling to Install

## Required

- **.NET SDK 9.x** (matching current target)
- **Docker Desktop** (or Docker Engine + Compose plugin)
- **Visual Studio 2022** with:
  - .NET MAUI workload
  - ASP.NET and web development workload
  - Container Tools

## Recommended CLI verification

- `dotnet --version`
- `docker --version`
- `docker compose version`

## NuGet packages (API side)

- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design`
- `Swashbuckle.AspNetCore` (recommended for API docs/testing)

---

## 8. Documentation Updates Needed

1. Update architecture section in `Documentation/SoftwareSpec.md`:
   - MAUI client + API + SQLite container split
   - responsibilities per layer
2. Update backlog in `Documentation/GitHubTasks.md`:
   - add epic/tasks for API project bootstrap
   - add epic/tasks for containerization and MAUI-to-API refactor
3. Add setup guide file:
   - local development prerequisites
   - how to run API + DB with Docker Compose
   - how to run MAUI client against local API
4. Add deployment notes:
   - environment variables
   - persistent volume requirements
   - migration strategy

---

## 9. Proposed Implementation Sequence

1. Add API + Shared.Contracts projects to solution.
2. Add API Dockerfile and docker-compose configuration.
3. Add EF Core SQLite setup and first migration.
4. Expose health + sample endpoint.
5. Wire MAUI client to call API through service abstraction.
6. Move one feature end-to-end as pilot.
7. Update docs and backlog after pilot validation.

---

## 10. Exit Criteria for Phase 1

- API runs in container via Docker Compose.
- SQLite data persists across container restarts via named volume.
- MAUI app successfully calls at least one API endpoint.
- Shared.Contracts is used by both client and backend.
- Documentation is updated for setup and architecture.
