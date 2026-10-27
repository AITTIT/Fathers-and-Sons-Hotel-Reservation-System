# Hotel Reservation System — Simple Database Schema (Draft)

## Goal

This schema is intentionally minimal so backend API development can proceed before Docker and full database implementation.

## Design principles

- Keep only core entities needed for reservation flow.
- Store payment status and provider reference, but **never store card details**.
- Use simple primary keys (`Id`) and foreign keys.
- Use UTC timestamps for auditability.

---

## 1. Users

Represents customers, staff, and admins.

| Field | Type | Required | Notes |
|---|---|---|---|
| Id | INTEGER | Yes | Primary key |
| Email | TEXT | Yes | Unique |
| PasswordHash | TEXT | Yes | Hashed password only |
| FirstName | TEXT | Yes |  |
| LastName | TEXT | Yes |  |
| Phone | TEXT | No |  |
| Role | TEXT | Yes | `Customer`, `Staff`, `Admin` |
| CreatedAtUtc | TEXT | Yes | ISO-8601 UTC |
| UpdatedAtUtc | TEXT | Yes | ISO-8601 UTC |

---

## 2. Hotels

Represents hotel properties.

| Field | Type | Required | Notes |
|---|---|---|---|
| Id | INTEGER | Yes | Primary key |
| Name | TEXT | Yes |  |
| AddressLine1 | TEXT | Yes |  |
| City | TEXT | Yes |  |
| Country | TEXT | Yes |  |
| PostalCode | TEXT | No |  |
| CreatedAtUtc | TEXT | Yes | ISO-8601 UTC |
| UpdatedAtUtc | TEXT | Yes | ISO-8601 UTC |

---

## 3. Rooms

Represents bookable rooms in a hotel.

| Field | Type | Required | Notes |
|---|---|---|---|
| Id | INTEGER | Yes | Primary key |
| HotelId | INTEGER | Yes | FK -> Hotels.Id |
| RoomNumber | TEXT | Yes | Unique per hotel |
| RoomType | TEXT | Yes | e.g. Single, Double, Suite |
| Capacity | INTEGER | Yes | Number of guests |
| PricePerNight | DECIMAL | Yes | Monetary value |
| IsActive | INTEGER | Yes | 1 = available for booking, 0 = disabled |
| CreatedAtUtc | TEXT | Yes | ISO-8601 UTC |
| UpdatedAtUtc | TEXT | Yes | ISO-8601 UTC |

---

## 4. Reservations

Represents room bookings made by users.

| Field | Type | Required | Notes |
|---|---|---|---|
| Id | INTEGER | Yes | Primary key |
| UserId | INTEGER | Yes | FK -> Users.Id |
| HotelId | INTEGER | Yes | FK -> Hotels.Id; must match the room's hotel |
| RoomId | INTEGER | Yes | FK -> Rooms.Id |
| CheckInDate | TEXT | Yes | Date (YYYY-MM-DD) |
| CheckOutDate | TEXT | Yes | Date (YYYY-MM-DD) |
| GuestCount | INTEGER | Yes | Must be <= Room.Capacity |
| Status | TEXT | Yes | `Pending`, `Confirmed`, `Cancelled` |
| TotalPrice | DECIMAL | Yes | Calculated total |
| CreatedAtUtc | TEXT | Yes | ISO-8601 UTC |
| UpdatedAtUtc | TEXT | Yes | ISO-8601 UTC |

---

## 5. Payments

Represents payment attempts/results for reservations.

| Field | Type | Required | Notes |
|---|---|---|---|
| Id | INTEGER | Yes | Primary key |
| ReservationId | INTEGER | Yes | FK -> Reservations.Id |
| Provider | TEXT | Yes | e.g. PayPal, MobilePay, Visa |
| ProviderTransactionId | TEXT | No | External reference |
| Amount | DECIMAL | Yes | Charged amount |
| Currency | TEXT | Yes | e.g. EUR |
| Status | TEXT | Yes | `Pending`, `Succeeded`, `Failed` |
| PaidAtUtc | TEXT | No | Set when succeeded |
| CreatedAtUtc | TEXT | Yes | ISO-8601 UTC |

> Note: card numbers/CVV/expiry must not be stored.

---

## 6. Notifications (optional simple table)

Stores outgoing notification history for booking events.

| Field | Type | Required | Notes |
|---|---|---|---|
| Id | INTEGER | Yes | Primary key |
| UserId | INTEGER | Yes | FK -> Users.Id |
| ReservationId | INTEGER | No | FK -> Reservations.Id |
| Type | TEXT | Yes | `BookingConfirmed`, `BookingCancelled`, etc. |
| Channel | TEXT | Yes | `Email`, `Push` |
| Status | TEXT | Yes | `Queued`, `Sent`, `Failed` |
| SentAtUtc | TEXT | No | Set when sent |
| CreatedAtUtc | TEXT | Yes | ISO-8601 UTC |

---

## Relationships (simple)

- **Hotel 1 -> many Rooms**
- **User 1 -> many Reservations**
- **Room 1 -> many Reservations**
- **Reservation 1 -> many Payments**
- **User 1 -> many Notifications**
- **Reservation 0..1 -> many Notifications**

---

## Minimum constraints to enforce

1. `Users.Email` unique.
2. `Rooms` unique `(HotelId, RoomNumber)`.
3. `Reservations.CheckInDate < Reservations.CheckOutDate`.
4. Prevent overlapping reservations for the same room when status is active (`Pending` or `Confirmed`).
5. `GuestCount > 0` and `GuestCount <= Rooms.Capacity`.
6. Monetary values (`PricePerNight`, `TotalPrice`, `Amount`) must be >= 0.

---

## Suggested implementation order

1. Users
2. Hotels
3. Rooms
4. Reservations
5. Payments
6. Notifications (optional in first backend skeleton)

This order allows API endpoints and unit tests to start with room browsing + reservation flow before full payment and notification features are finalized.
