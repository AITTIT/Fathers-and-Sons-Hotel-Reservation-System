# Hotel Reservation Backend

ASP.NET Core (.NET 9) Web API backed by PostgreSQL. It is client-agnostic: the MAUI app and browsers use the same HTTP API.

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

Schema changes are EF Core migrations ([`Migrations/`](HotelReservation.Api/Migrations)), applied automatically at startup. The overlap exclusion constraint and capacity trigger from `InitialCreate` are dropped by `RemoveReservationRules` until the booking rules return. The design is in [`../Documentation/DatabaseSchema.md`](../Documentation/DatabaseSchema.md).

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

With `Seed:Demo=true` (`Seed__Demo`, on in `docker-compose.yml`), an empty database also gets 3 hotels, 24 rooms (one inactive), and the demo staff and customer accounts above. It runs only in the Development environment and only when there are no hotels yet, in one transaction. Set it to `false` in Compose for an empty system. Demo data is not added to a database that already has hotels; `docker compose down -v` to start clean.

## Configuration

`appsettings.Development.json` holds the local defaults. Outside Development, supply values through environment variables or a secret store, never source control.

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | Npgsql connection string |
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

Reservations and payments are out of scope for now, so the API has no endpoints for them. The `Reservation` and `Payment` models and their tables remain, and deleting a room or hotel still checks them. Business logic belongs in this API when they return. The previous endpoints are in commit `60c4c19` (`ReservationEndpoints.cs`, `PaymentEndpoints.cs`) if you want to reuse them.

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

- A production profile (no dev settings, HTTPS reverse proxy, unpublished Postgres port)
- Payment provider and refunds (simulated?)
- Email delivery for password reset
- Automated tests
- Notifications table (optional in the schema doc)

## Known issues
- Everything is Development-only. The credentials, the dev reset token and the docs are for local use, and Postgres is published on 5432. It's fine for development and not for deployment.
- Password reset only works in Development, because it returns a dev token instead of sending email.
- No automated tests and no CI yet.