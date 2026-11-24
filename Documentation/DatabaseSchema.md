# Hotel Reservation System — Current Database Schema

## Database and schema ownership

The backend uses PostgreSQL with Entity Framework Core and the Npgsql provider. The API project owns the EF Core model and migrations; migrations are applied automatically when the API starts. The development Docker Compose setup uses PostgreSQL 17 and persists data in the `postgres-data` named volume.

This document describes the schema currently represented by `backend/HotelReservation.Api/Data/AppDbContext.cs` and its migrations. It is not a proposed future booking schema.

## PostgreSQL conventions

- Primary keys are `integer` identity columns.
- Strings use PostgreSQL `text`.
- UTC date/time values use `timestamp with time zone`.
- `Rooms.PricePerNight` uses `numeric(10,2)`.
- `Rooms.IsActive` uses `boolean`.
- EF Core creates indexes and foreign keys as described below.

## Tables

### `Users`

Customer, staff, and administrator accounts.

| Column | PostgreSQL type | Required | Notes |
|---|---|---:|---|
| `Id` | `integer` | Yes | Primary key, identity |
| `Email` | `text` | Yes | Unique; API normalizes to lowercase |
| `PasswordHash` | `text` | Yes | BCrypt hash; plaintext passwords are not stored |
| `FirstName` | `text` | Yes | |
| `LastName` | `text` | Yes | |
| `Phone` | `text` | No | |
| `Role` | `text` | Yes | `Customer`, `Staff`, or `Admin` |
| `CreatedAtUtc` | `timestamp with time zone` | Yes | |
| `UpdatedAtUtc` | `timestamp with time zone` | Yes | |

Constraints and indexes:

- Unique index on `Email`.
- Check constraint limits `Role` to `Customer`, `Staff`, or `Admin`.

### `Hotels`

Hotel properties.

| Column | PostgreSQL type | Required | Notes |
|---|---|---:|---|
| `Id` | `integer` | Yes | Primary key, identity |
| `Name` | `text` | Yes | |
| `AddressLine1` | `text` | Yes | |
| `City` | `text` | Yes | |
| `Country` | `text` | Yes | |
| `PostalCode` | `text` | No | |
| `CreatedAtUtc` | `timestamp with time zone` | Yes | |
| `UpdatedAtUtc` | `timestamp with time zone` | Yes | |

### `Rooms`

Rooms belonging to a hotel.

| Column | PostgreSQL type | Required | Notes |
|---|---|---:|---|
| `Id` | `integer` | Yes | Primary key, identity |
| `HotelId` | `integer` | Yes | Foreign key to `Hotels.Id` |
| `RoomNumber` | `text` | Yes | Unique within a hotel |
| `RoomType` | `text` | Yes | |
| `Capacity` | `integer` | Yes | Must be greater than zero |
| `PricePerNight` | `numeric(10,2)` | Yes | Must be zero or greater |
| `IsActive` | `boolean` | Yes | Defaults to `true` in the application model |
| `CreatedAtUtc` | `timestamp with time zone` | Yes | |
| `UpdatedAtUtc` | `timestamp with time zone` | Yes | |

Constraints and indexes:

- Foreign key `HotelId` references `Hotels.Id`; deleting a hotel cascades to its rooms.
- Unique index on `(HotelId, RoomNumber)`.
- Check constraints require `Capacity > 0` and `PricePerNight >= 0`.

### `PasswordResetTokens`

Single-use password reset tokens. Only the SHA-256 token hash is stored; the raw token is returned only in Development because email delivery is not implemented.

| Column | PostgreSQL type | Required | Notes |
|---|---|---:|---|
| `Id` | `integer` | Yes | Primary key, identity |
| `UserId` | `integer` | Yes | Foreign key to `Users.Id` |
| `TokenHash` | `text` | Yes | Unique |
| `ExpiresAtUtc` | `timestamp with time zone` | Yes | |
| `UsedAtUtc` | `timestamp with time zone` | No | Null until consumed |
| `CreatedAtUtc` | `timestamp with time zone` | Yes | |

Constraints and indexes:

- Foreign key `UserId` references `Users.Id` with cascade delete.
- Unique index on `TokenHash`; index on `UserId`.

### `RefreshTokens`

Hashed, one-use refresh tokens associated with user sessions.

| Column | PostgreSQL type | Required | Notes |
|---|---|---:|---|
| `Id` | `integer` | Yes | Primary key, identity |
| `UserId` | `integer` | Yes | Foreign key to `Users.Id` |
| `TokenHash` | `text` | Yes | Unique; raw token is sent to the client |
| `ExpiresAtUtc` | `timestamp with time zone` | Yes | |
| `UsedAtUtc` | `timestamp with time zone` | No | Set when exchanged |
| `RevokedAtUtc` | `timestamp with time zone` | No | Set on logout, password changes, or token reuse |
| `CreatedAtUtc` | `timestamp with time zone` | Yes | |

Constraints and indexes:

- Foreign key `UserId` references `Users.Id` with cascade delete.
- Unique index on `TokenHash`; index on `UserId`.

## Relationships

- One `Hotel` has many `Rooms`.
- Each `Room` belongs to one `Hotel`.
- One `User` has many `PasswordResetTokens`.
- One `User` has many `RefreshTokens`.

## Not currently persisted

The current EF Core model does **not** contain reservation, payment, or notification tables. The API has no reservation or payment endpoints. Room availability is not yet derived from bookings. These capabilities remain future work; do not treat them as part of the current database schema.

Payment card details must never be stored by this application. If payment processing is added, use an external provider and persist only the minimum non-sensitive provider reference and status needed by the application.

## Schema changes

Update the API entities and `AppDbContext`, then create and review an EF Core migration. The API applies pending migrations at startup. See [the backend README](../backend/README.md) for migration and local database commands.
