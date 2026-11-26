# Hotel Reservation Backend

ASP.NET Core (.NET 9) Web API backed by PostgreSQL (default) or MySQL/MariaDB, chosen by configuration. It is client-agnostic: the MAUI app and browsers use the same HTTP API.

> **Status:** early development. Auth, users, hotels and rooms are implemented. Reservations and payments are out of scope for now. The API and database run together in Docker Compose.

## Quick start

Requires only Docker. (The .NET 9 SDK is needed only to run or debug the API outside Docker.)

```powershell
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
| `admin@hotel.com` | `hoteladmin` | Admin |
| `staff@hotel.com` | `hoteldemo` | Staff |
| `customer1@hotel.com`, `customer2@hotel.com` | `hoteldemo` | Customer |

Common tasks:

```powershell
docker compose logs -f api          # API logs
docker compose up -d --build api    # after pulling backend changes
docker compose down -v              # wipe the database; the next start re-creates the admin and demo data
```

Compose runs the API in Development mode with local-only settings (see `docker-compose.yml`). `GET /health` is a liveness check.

### Running on MariaDB

The `mariadb` profile adds a second API next to the PostgreSQL one, backed by MariaDB 10.4.12 (the school server's version). The same accounts work on both.

```powershell
docker compose --profile mariadb up -d --build
docker compose --profile mariadb down -v   # wipe both databases; without --profile the MariaDB services are left alone
```

| What | Where |
|---|---|
| API on MariaDB | http://localhost:5048 (docs at `/scalar/v1`) |
| Test page against it | http://localhost:5500/?api=http://localhost:5048 |
| MariaDB | `localhost:3306`, db `hotel_reservation`, `hotel`/`hotel` (root: `root`/`root`) |

The first MariaDB start takes 20–30 seconds; the API waits for its healthcheck. See [Database](#database) for how the API picks its database and how to use an external one.

## Integration tests

The backend integration tests use `WebApplicationFactory` and Testcontainers. Each test run starts an isolated PostgreSQL 17 container, applies the EF Core migrations, runs the tests, and removes the temporary container afterward. The tests do not use or modify the PostgreSQL database from Docker Compose.

Requires the .NET 9 SDK and Docker Desktop (or another running Docker daemon). From the repository root, run:

```powershell
dotnet test .\backend\HotelReservation.Api.Tests\HotelReservation.Api.Tests.csproj
```

To run only the room endpoint tests:

```powershell
dotnet test .\backend\HotelReservation.Api.Tests\HotelReservation.Api.Tests.csproj --filter "FullyQualifiedName~RoomEndpointTests"
```

Docker Compose does not need to be running for these tests. If Docker is unavailable, Testcontainers cannot start the temporary PostgreSQL database. The tests run on PostgreSQL only; MySQL/MariaDB support was verified by hand (see [Database](#database)).

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

Neither is exposed outside Development. Summaries and access notes are `.WithSummary`/`.WithDescription` calls on each endpoint; update them when you change an endpoint.

General conventions:

- JSON over HTTP, `camelCase`. Protected endpoints need `Authorization: Bearer <token>`.
- List endpoints return `{ items, total, page, pageSize }` (default page size 25, max 100).
- Errors are `{ "error": "..." }`, or a validation problem document (400).
- Roles: **Customer** (own account), **Staff** (manage rooms, list customer accounts), **Admin** (everything, including users and hotels).
- Access tokens last 15 minutes. `POST /api/auth/refresh` exchanges a refresh token (30 days, one use) for a new pair, and `POST /api/auth/logout` revokes one.
- Reusing a refresh token ends all of the user's sessions, so clients must only ever send one refresh at a time. A password change or reset ends them too. Either way, an access token already issued keeps working until it expires. Store refresh tokens securely, such as in `SecureStorage` on MAUI.
- Password reset has no email delivery yet. In Development `forgot-password` returns the token as `devToken`; elsewhere it is never returned.
- There are no reservation or payment endpoints for now. See [Deferred work](#deferred-work).

## Database

The schema is described in [`../Documentation/DatabaseSchema.md`](../Documentation/DatabaseSchema.md). The API creates and updates it itself with EF Core migrations at startup.

### Supported databases

| Database | Provider | Verified with |
|---|---|---|
| PostgreSQL 13 or newer | `Postgres` (default) | 17 (Compose, integration tests) |
| MariaDB 10.4 or newer | `MySql` | 10.4.12 (Compose `mariadb` profile and the school server) |
| MySQL 8.0.16 or newer | `MySql` | 8.4.13 (one-off container) |

MySQL and MariaDB share the `MySql` provider (Pomelo); `Database:ServerVersion` tells them apart. MySQL older than 8.0.16 accepts but does not enforce check constraints, so the database would no longer reject an invalid role, capacity or price.

### Choosing the database

| Setting (env var) | Value |
|---|---|
| `Database:Provider` (`Database__Provider`) | `Postgres` (default) or `MySql` |
| `Database:ServerVersion` (`Database__ServerVersion`) | `MySql` only, required: version and type, e.g. `10.4.12-mariadb` or `8.4.13-mysql`. Run `SELECT VERSION();` on the server (MariaDB may report `5.5.5-10.4.12-MariaDB…`; use the part after `5.5.5-`) |
| `ConnectionStrings:Default` (`ConnectionStrings__Default`) | See below |

Connection string formats:

```text
Postgres: Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<password>
MySql:    Server=<host>;Port=3306;Database=<db>;User=<user>;Password=<password>
```

An unknown provider, a missing connection string or a missing/invalid server version stops the API at startup with a message saying which.

### Database requirements

- The database must already exist; the API creates the tables, not the database.
- The API's database user needs full rights on that database: creating and altering tables, indexes and foreign keys, plus reading and writing data. A per-user school account with `ALL PRIVILEGES` on its own databases is enough.
- **MySQL/MariaDB:** create the database with character set `utf8mb4`, collation `utf8mb4_unicode_520_ci`. The API sets both on its own tables anyway, but avoid the `latin1` server default and the 3-byte `utf8`. Comparisons are case- and accent-insensitive there (`Malmo` matches `Malmö`); on PostgreSQL they are not.
- The API must be able to reach the server. At startup it tries to connect 10 times, 2 seconds apart, logging `Database not ready` with the reason each time, then stops. Migration errors are not retried.

### Using an external database

For example the school MariaDB (`mariadb.vamk.fi`, reachable on campus or over the school VPN). Keep the connection string in environment variables or user secrets, never in a committed file:

```powershell
cd backend\HotelReservation.Api
$pw = Read-Host "Database password" -AsSecureString
$env:ConnectionStrings__Default = "Server=mariadb.vamk.fi;Port=3306;Database=<db>;User=<user>;Password=" + [Net.NetworkCredential]::new('', $pw).Password
$env:Database__Provider = "MySql"
$env:Database__ServerVersion = "10.4.12-mariadb"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5998"
dotnet run --no-launch-profile
```

The settings last only for that window. Use the server's public name: the internal address a web database tool shows (such as `172.17.x.x`) is not reachable from outside. Point the test page at it with `http://localhost:5500/?api=http://localhost:5998`.

### Migrations

Each provider has its own migrations: [`Migrations/Postgres`](HotelReservation.Api/Migrations/Postgres) (`PostgresAppDbContext`) and [`Migrations/MySql`](HotelReservation.Api/Migrations/MySql) (`MySqlAppDbContext`). Both are subclasses of `AppDbContext`, which holds the model; the rest of the API only uses `AppDbContext`.

**Until launch there is exactly one migration per provider, `InitialCreate`.** After changing entities or `AppDbContext`, regenerate it instead of adding a migration, then recreate your local databases:

```powershell
cd backend
./scripts/migrations.ps1                                  # regenerate InitialCreate for both providers
docker compose --profile mariadb down -v                  # recreate local databases (seed data comes back)
docker compose --profile mariadb up -d --build
```

After launch, databases hold real data, so schema changes must become normal migrations added on top of `InitialCreate`.

Other script modes (no database needed for `-Check`):

```powershell
./scripts/migrations.ps1 -Check     # fails if the model changed but the migrations did not, for either provider
./scripts/migrations.ps1 -Apply -Provider MySql -ServerVersion 10.4.12-mariadb -Connection "Server=...;Database=...;User=...;Password=..."
```

The script uses the `dotnet-ef` version pinned in [`dotnet-tools.json`](dotnet-tools.json) and works in Windows PowerShell and `pwsh`. Without a connection, `dotnet ef` uses the design-time factories in `Data/ProviderDbContexts.cs`, so it needs no database or secrets.

### Local databases

Reset (**deletes all data**; the admin and demo data are seeded again):

```powershell
cd backend
docker compose down -v                        # PostgreSQL only
docker compose --profile mariadb down -v      # PostgreSQL and MariaDB
```

Local Postgres: `localhost:5432`, database `hotel_reservation`, user/password `postgres`/`postgres`. Local MariaDB: `localhost:3306`, same database name, `hotel`/`hotel` (root `root`/`root`). Development credentials only.

### Default admin

If no Admin exists at startup, one is created from configuration.

| Setting (env var) | Purpose |
|---|---|
| `Seed:Admin:Email` (`Seed__Admin__Email`) | Admin email |
| `Seed:Admin:Password` (`Seed__Admin__Password`) | Must meet the password rules (8 characters to 72 bytes) |
| `Seed:Admin:FirstName`, `Seed:Admin:LastName` | Optional |

Development uses `default-admin@change.me` / `hoteladmin`. Without this config and without an admin, the API logs a warning and starts with no users.

### Demo data

With `Seed:Demo=true` (`Seed__Demo`, on in `docker-compose.yml`), an empty database also gets 3 hotels, 24 rooms (one inactive), and the demo staff and customer accounts above. It runs only in the Development environment and only when there are no hotels yet, in one transaction. Set it to `false` in Compose for an empty system. Demo data is not added to a database that already has hotels; `docker compose down -v` to start clean.

## Configuration

`appsettings.Development.json` holds the local defaults. Outside Development, supply values through environment variables or a secret store, never source control.

| Key | Purpose |
|---|---|
| `Database:Provider` | `Postgres` (default) or `MySql`. See [Choosing the database](#choosing-the-database) |
| `Database:ServerVersion` | Required for `MySql`, e.g. `10.4.12-mariadb` |
| `ConnectionStrings:Default` | Connection string for the chosen provider. **Required** |
| `Jwt:Key` | Signing key. **Required, at least 32 characters** |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, `Jwt:RefreshTokenDays` | Optional; default `HotelReservation.Api`, `HotelReservation.Client`, `15`, `30` |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API. Empty means no cross-origin access. MAUI is unaffected |
| `RateLimiting:AuthPermitLimit`, `RateLimiting:AuthWindowSeconds` | Per-IP limit on register, login, refresh, forgot/reset/change-password; default `10` per `60` s, then 429 with `Retry-After`. Behind a reverse proxy, configure forwarded headers so the real client IP is used |

## Development test page

[`test-client/index.html`](test-client/index.html) lives outside the API project and calls it over HTTP like a real client. It has a login screen with quick login for the seeded accounts, plus a tab each for hotels, rooms, users and your account. Lists have filters, paging and per-row actions, and a bar at the bottom logs every API call. Expired access tokens are refreshed automatically, and Log out revokes the refresh token. Raw mode also shows actions your role cannot use and skips browser validation, to test the API's own 400, 401 and 403 responses. Compose serves it on http://localhost:5500 (nginx, with the folder mounted so edits show on refresh); that origin is already in the API's CORS list. To serve it yourself instead:

```powershell
python -m http.server 5500 --directory test-client
```

The API URL defaults to `http://localhost:5047`; override it with `?api=http://host:port`.

## Tips

- If `dotnet build` fails copying `HotelReservation.Api.exe`, a running API instance is locking it. Stop it first.

## Deferred work

Reservations and payments are out of scope for now, so the API has no endpoints, models or tables for them. Business logic belongs in this API when they return. The previous endpoints are in commit `60c4c19` (`ReservationEndpoints.cs`, `PaymentEndpoints.cs`), and the models and their `AppDbContext` configuration are in commit `6cda6cd`, if you want to reuse them.

Reservations:

- Reservation endpoints
- Calculate `totalPrice` on the server from the room's nightly price and re-calculate it on changes
- Prevent overlapping bookings for a room, including a database constraint for simultaneous requests
- Reject guest counts above room capacity, past check-in dates and inactive rooms
- Status flow: confirm only `Pending`, no changes after `Cancelled`
- Customers can't change or cancel a stay that has started

Payments:

- Payment endpoints
- Calculate `amount` on the server from what is still owed
- Reject payments for cancelled or fully paid reservations, and allow one pending payment per reservation
- Restrict providers to Visa, PayPal and MobilePay
- A successful payment confirms a `Pending` reservation

## Not done yet

- A production profile (no dev settings, HTTPS reverse proxy, unpublished database ports)
- Payment provider and refunds (simulated?)
- Email delivery for password reset
- Notifications table (optional in the schema doc)

## Known issues
- Everything is Development-only. The credentials, the dev reset token and the docs are for local use, and the databases are published on 5432 (and 3306 with the `mariadb` profile). It's fine for development and not for deployment.
- Password reset only works in Development, because it returns a dev token instead of sending email.
- The GitHub Actions workflow runs the backend integration tests on every push; pull-request-specific checks are not configured yet.