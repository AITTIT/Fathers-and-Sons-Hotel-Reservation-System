# Security Review

Date: 2026-10-27. Scope: `backend/` (ASP.NET Core API, SQL init scripts, Docker Compose) and `test-client/index.html`.

No authorization, injection or JWT flaws were found in the API code. Findings are in the test client and local development setup.

| # | Severity | File | Lines | Vulnerability | Confidence |
|---|----------|------|-------|---------------|------------|
| 1 | HIGH | test-client/index.html | 100, 213-217 | The `?api=` URL parameter lets a crafted link send a logged-in user's bearer token to an attacker's server | 9/10 |
| 2 | MEDIUM | backend/docker-compose.yml | 8-11 | Postgres is published on all host interfaces with the fixed password `postgres` | 9/10 |
| 3 | LOW | backend/HotelReservation.Api/Features/Users/AuthEndpoints.cs | 25-27 | Registration returns 409 for an existing email, revealing which emails have accounts | 10/10 |

## Details

### 1. Token sent to an arbitrary API host (HIGH)
The test client takes its API base URL from `?api=` without validation. `restoreSession` then sends the token stored in `localStorage` to that URL. A crafted link can point the page at an attacker's server, which can allow the page's origin through CORS and capture the token.

Fix: remove the override, or restrict it to a fixed allow-list of origins before any request carries a token.

### 2. Postgres exposed on all interfaces (MEDIUM)
`docker-compose.yml` publishes `5432:5432` with `postgres` / `postgres`. Anyone who can reach the machine can read or modify the database.

Fix: bind to `127.0.0.1:5432:5432`, or drop the published port and use a private Docker network. Use unique credentials for any network-accessible deployment.

### 3. Email enumeration through registration (LOW)
`POST /api/auth/register` answers 409 when the email exists. This is a deliberate usability trade-off; a verification-email flow would give a uniform response.

## Not flagged by the review, but open

- No rate limiting or lockout on login, register or password reset.
- Tokens stay valid after a password change until they expire (60 minutes).
- The committed `Jwt:Key` is a development placeholder. Production needs a real secret from the environment or a secret store.
- No email delivery for password reset; the token is returned as `devToken` in Development only.
