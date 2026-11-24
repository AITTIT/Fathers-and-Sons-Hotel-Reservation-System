# MAUI Client, API, and Container Architecture

## Current choices

- **Client:** .NET MAUI, organized around feature-specific Views, ViewModels, models, and service abstractions.
- **Backend:** ASP.NET Core Web API on .NET 9. Business rules and persistence belong in the API, not the MAUI client.
- **Persistence:** PostgreSQL 17, accessed by EF Core through Npgsql. The API applies migrations at startup.
- **Local orchestration:** Docker Compose starts PostgreSQL, the API, and a small Nginx-hosted browser test client. PostgreSQL data is stored in the `postgres-data` named volume.
- **Authentication:** The API implements JWT access tokens, rotating refresh tokens, password reset tokens, and Customer/Staff/Admin authorization. The current MAUI authentication service is still an in-memory mock.

SQLite is not used by the current application.

## Architecture

```text
.NET MAUI client
  Views / ViewModels / feature service abstractions
  Current auth and room services are in-memory mocks
                |
                | HTTP + JSON (target integration)
                v
ASP.NET Core Web API
  Authentication, authorization, validation, hotel/room/user endpoints
  EF Core migrations and data access
                |
                | Npgsql
                v
PostgreSQL 17
  Docker Compose service; postgres-data named volume
```

The API is client-agnostic: browser and MAUI clients use the same HTTP endpoints. The API currently exposes health, authentication, user, hotel, and room routes. Reservations, payments, and notification delivery are not implemented.

## Project boundaries and integration status

### MAUI client

- Keep XAML pages and presentation state in the client.
- ViewModels depend on feature service interfaces, not on EF Core or database classes.
- `MockAuthService` and `MockRoomService` currently provide in-memory behavior; they are registered by `MauiProgram`.
- Replace those mock implementations with API-backed services when client/server integration is implemented.
- Several destinations, including bookings and staff reservations/rooms/customers, are placeholder pages.

### API and persistence

- API endpoints enforce validation and role-based access.
- EF Core entities, `AppDbContext`, and migrations are owned by `backend/HotelReservation.Api`.
- PostgreSQL connection configuration is supplied through `ConnectionStrings:Default`.
- The Compose API waits for PostgreSQL health and applies migrations during startup.

### Shared contracts

A shared contracts library is a target boundary for request/response DTOs, but no such project is present yet. At present, API contract records are defined in the API feature folders and client-facing types are defined in the MAUI project. Introduce a shared library as part of the future API-backed client integration rather than assuming it is already in use.

## Local development

From `backend`:

```powershell
docker compose up -d --build
```

The Compose stack exposes:

| Service | Local address |
|---|---|
| API | `http://localhost:5047` |
| Interactive API docs (Development only) | `http://localhost:5047/scalar/v1` |
| OpenAPI document (Development only) | `http://localhost:5047/openapi/v1.json` |
| Browser test client | `http://localhost:5500` |
| PostgreSQL | `localhost:5432` |

Development Compose values, including credentials and signing key, are for local use only. Do not reuse them outside development. `docker compose down -v` removes the named database volume and permanently deletes its data.

The MAUI client does not yet call this Compose API. The browser test client exercises the HTTP API while the MAUI app continues to use its mock services.

For backend setup, API behavior, test commands, configuration, migration commands, and known limitations, see [the backend README](../backend/README.md).

## Current implementation versus remaining work

Implemented:

- ASP.NET Core API and PostgreSQL Compose service.
- PostgreSQL persistence, EF Core migrations, startup migration application, and development data seeding.
- API authentication/authorization and user, hotel, and room endpoints.
- MAUI room-search and staff-dashboard screens, plus mock sign-in/registration.

Remaining:

- Wire MAUI auth and room services to API-backed implementations.
- Add the shared contracts project and use its DTOs across the client/API boundary.
- Implement reservation and payment models/endpoints and corresponding client flows.
- Replace mock staff dashboard data and placeholder pages with API-backed screens.
- Add a production deployment profile and production-safe secrets, HTTPS, and database exposure settings.
