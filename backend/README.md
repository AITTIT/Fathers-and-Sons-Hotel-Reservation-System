# Hotel Reservation Backend

ASP.NET Core (.NET 9) Web API backed by PostgreSQL. It is client-agnostic: the MAUI app and browsers use the same HTTP API.

```
Client (MAUI / browser)  ->  ASP.NET Core API  ->  EF Core / Npgsql  ->  PostgreSQL (Docker)
```

> **Status:** early development. Auth, users, hotels, rooms, reservations and payments (simulated, no real provider) are implemented. The API and database run together in Docker Compose.

## Quick start (teammates start here)

Requires only Docker. (The .NET 9 SDK is needed only to run or debug the API outside Docker.)

```powershell
cd backend
docker compose up -d --build
```

| What | Where |
|---|---|
| API | http://localhost:5047 |
| Interactive API docs | http://localhost:5047/scalar/v1 |
| OpenAPI document (for client generation) | http://localhost:5047/openapi/v1.json |
| Test page | http://localhost:5500 |
| PostgreSQL | `localhost:5432`, db `hotel_reservation`, `postgres`/`postgres` |

The first start creates the schema, the default admin and some demo data. Log in with any of these:

| Account | Password | Role |
|---|---|---|
| `default-admin@change.me` | `hoteladmin` | Admin |
| `demo-staff@change.me` | `hoteldemo` | Staff |
| `demo-customer1@change.me`, `demo-customer2@change.me` | `hoteldemo` | Customer |

Common tasks:

```powershell
docker compose logs -f api          # API logs
docker compose up -d --build api    # after pulling backend changes
docker compose down -v              # wipe the database; the next start re-creates the admin and demo data
```

Compose runs the API in Development mode with local-only settings (see `docker-compose.yml`). `GET /health` is a liveness check.

To debug the API from the IDE instead, stop the container (both use port 5047) and run it yourself against the same database:

```powershell
docker compose stop api
cd HotelReservation.Api
dotnet run --launch-profile http
```

## API documentation

In Development, the API serves its own documentation:

- **Interactive docs:** `http://localhost:5047/scalar/v1`. Every endpoint, its access rules, parameters and request/response shapes. Log in via `/api/auth/login` and paste the token under Bearer authentication to try protected calls.
- **OpenAPI document:** `http://localhost:5047/openapi/v1.json`. Use it to generate a client (for example for MAUI).

Neither is exposed outside Development. The summaries and access notes are in [`OpenApiSetup.cs`](HotelReservation.Api/Features/Common/OpenApiSetup.cs); update them when you change an endpoint.

General conventions:

- JSON over HTTP, `camelCase`. Protected endpoints need `Authorization: Bearer <token>`.
- List endpoints return `{ items, total, page, pageSize }` (default page size 25, max 100).
- Errors are `{ "error": "..." }`, or a validation problem document (400).
- Roles: **Customer** (own account, reservations and payments; other people's are 404), **Staff** (read access to everything operational, manage rooms and reservations), **Admin** (everything, including users and hotels).
- Tokens last 60 minutes and stay valid after a password change.
- Password reset has no email delivery yet. In Development `forgot-password` returns the token as `devToken`; elsewhere it is never returned.
- Payments are simulated: staff "complete" a payment to stand in for a provider callback. There are no refunds, and reservations are cancelled, never deleted.

## Database

Schema changes are EF Core migrations ([`Migrations/`](HotelReservation.Api/Migrations)), applied automatically at startup. The overlap exclusion constraint, capacity trigger and `btree_gist` extension are raw SQL inside `InitialCreate`. The design is in [`../Documentation/DatabaseSchema.md`](../Documentation/DatabaseSchema.md).

```powershell
cd backend\HotelReservation.Api
dotnet ef migrations add <Name> --configuration Release    # after changing entities or AppDbContext
```

Reset the database (**deletes all data**):

```powershell
cd backend
docker compose down -v
docker compose up -d
```

Local Postgres: `localhost:5432`, database `hotel_reservation`, user/password `postgres`/`postgres`. Development credentials only.

### Default admin

If no Admin exists at startup, one is created from configuration.

| Setting (env var) | Purpose |
|---|---|
| `Seed:Admin:Email` (`Seed__Admin__Email`) | Admin email |
| `Seed:Admin:Password` (`Seed__Admin__Password`) | Must meet the password rules (8 characters to 72 bytes) |
| `Seed:Admin:FirstName`, `Seed:Admin:LastName` | Optional |

Development uses `default-admin@change.me` / `hoteladmin`. Without this config and without an admin, the API logs a warning and starts with no users.

### Demo data

With `Seed:Demo=true` (`Seed__Demo`, on in `docker-compose.yml`), an empty database also gets 3 hotels, 24 rooms (one inactive), the demo staff and customer accounts above, 3 reservations and 2 payments. It runs only in the Development environment and only when there are no hotels yet, in one transaction. Set it to `false` in Compose for an empty system. Demo data is not added to a database that already has hotels; `docker compose down -v` to start clean.

## Configuration

`appsettings.Development.json` holds the local defaults. Outside Development, supply values through environment variables or a secret store, never source control.

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | Npgsql connection string |
| `Jwt:Key` | Signing key. **Required, at least 32 characters** |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | Optional; default `HotelReservation.Api`, `HotelReservation.Client`, `60` |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API. Empty means no cross-origin access. MAUI is unaffected |
| `RateLimiting:AuthPermitLimit`, `RateLimiting:AuthWindowSeconds` | Per-IP limit on register, login, forgot/reset/change-password; default `10` per `60` s, then 429 with `Retry-After`. Behind a reverse proxy, configure forwarded headers so the real client IP is used |

## Development test page

[`test-client/index.html`](../test-client/index.html) lives outside the API project and calls it over HTTP like a real client. It has forms for the auth, user, hotel, room, reservation and payment endpoints, plus tables of what the logged-in role can see. Compose serves it on http://localhost:5500 (nginx, with the folder mounted so edits show on refresh); that origin is already in the API's CORS list. To serve it yourself instead:

```powershell
python -m http.server 5500 --directory ..\test-client
```

The API URL defaults to `http://localhost:5047`; override it with `?api=http://host:port`.

## Tips

- If `dotnet build` fails copying `HotelReservation.Api.exe`, a running API instance is locking it. Stop it first.
- The `https` launch profile listens on `https://localhost:7170` (trust the dev certificate with `dotnet dev-certs https --trust`).

## Not done yet

- A production profile (no dev settings, HTTPS reverse proxy, unpublished Postgres port)
- Payment provider and refunds (simulated?)
- Email delivery for password reset
- Automated tests
- Notifications table (optional in the schema doc)
