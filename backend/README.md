# Hotel Reservation Backend

ASP.NET Core (.NET 9) Web API backed by PostgreSQL. The MAUI client talks to this API over HTTP; all persistence and business logic live here.

```
MAUI app  ->  ASP.NET Core API  ->  EF Core / Npgsql  ->  PostgreSQL (Docker)
```

> **Status:** early development. The database schema, auth and user management are in place. Hotel, room, reservation and payment endpoints are not built yet (the tables and entities exist). The API itself is not containerized yet; the `Dockerfile` is empty.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) with Compose

## Quick start

```powershell
# 1. Start PostgreSQL (creates and seeds the database on first start)
cd backend
docker compose up -d

# 2. Run the API
cd HotelReservation.Api
dotnet run --launch-profile http
```

`GET /health` returns a plain-text liveness message. For the development test page, see below.

## Layout

```
backend/
  docker-compose.yml           PostgreSQL 17 container
  db/init/                     SQL run on first database start (schema + seed data)
  HotelReservation.Api/
    Program.cs                 service registration, auth, endpoint mapping
    Data/AppDbContext.cs       EF Core context
    Data/DevEndpoints.cs       development-only table viewer API
    Features/<Feature>/        entities, DTOs and endpoints, grouped by feature
    Features/Common/           paging and authorization policies
```

## Database

PostgreSQL runs in Docker with these settings (defined in `docker-compose.yml`):

| Setting | Value |
|---|---|
| Host / port | `localhost:5432` |
| Database | `testdb` |
| User / password | `postgres` / `postgres` |

These are local development credentials only. Never reuse them anywhere else.

### Schema and seed data

The schema is created by plain SQL, not EF migrations. Postgres runs every file in `db/init/` in alphabetical order, **only when the data volume is empty**:

| File | Contents |
|---|---|
| `01-users.sql` | `Users`, 4 test users |
| `02-hotels.sql` | `Hotels`, 5 hotels |
| `03-rooms.sql` | `Rooms`, 7 per hotel (35) |
| `04-reservations.sql` | `Reservations`, 8 sample bookings |
| `05-payments.sql` | `Payments`, 9 sample payments |
| `06-password-reset-tokens.sql` | `PasswordResetTokens` (empty) |

The design is documented in [`../Documentation/DatabaseSchema.md`](../Documentation/DatabaseSchema.md).

Relationships: Hotel 1-N Rooms, User 1-N Reservations, Room 1-N Reservations, Reservation 1-N Payments. A reservation also stores `HotelId`, and a composite foreign key guarantees it matches the room's hotel.

Rules enforced by the database:

- `Users.Email` is unique; `Role` is `Customer`, `Staff` or `Admin`.
- `(HotelId, RoomNumber)` is unique per room.
- `CheckInDate < CheckOutDate`.
- Active (`Pending`/`Confirmed`) reservations cannot overlap on the same room. The check-out day is free for the next check-in, and `Cancelled` bookings don't block.
- `GuestCount > 0` and cannot exceed the room's capacity (trigger).
- Money values are `>= 0`. A payment has `PaidAtUtc` exactly when its status is `Succeeded`.

### Resetting the database

To change the schema or re-seed, wipe the volume and start again. **This deletes all data.**

```powershell
cd backend
docker compose down -v
docker compose up -d
```

Reservation dates in the seed are relative to the current date.

### Test users

All use the password `Password123!`.

| Email | Role |
|---|---|
| `admin@hotel.test` | Admin |
| `staff@hotel.test` | Staff |
| `customer1@hotel.test` | Customer |
| `customer2@hotel.test` | Customer |

## Configuration

`appsettings.Development.json` provides the local defaults:

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | Npgsql connection string |
| `Jwt:Key` | HMAC signing key. **Required, at least 32 characters**; the API refuses to start without it |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | Optional; default `HotelReservation.Api`, `HotelReservation.Client`, `60` |
| `Cors:AllowedOrigins` | Array of browser origins allowed to call the API (e.g. `http://localhost:5500`). Empty means no cross-origin browser access. MAUI is unaffected |

The committed `Jwt:Key` is a development placeholder. Outside development, supply real values through environment variables (e.g. `Jwt__Key`, `ConnectionStrings__Default`) or a secret store, never in source control.

## API

JSON over HTTP. Protected endpoints need `Authorization: Bearer <token>`. Errors use `{ "error": "..." }`, or standard problem details with field errors for validation (400).

### Auth

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | none | Create a `Customer` account. 201, 400 (validation), 409 (email taken) |
| POST | `/api/auth/login` | none | Returns `{ token, expiresAtUtc, user }`. 401 for any bad credentials |
| GET | `/api/auth/me` | token | Current user |
| POST | `/api/auth/forgot-password` | none | Always 200 with the same message. See the note below |
| POST | `/api/auth/reset-password` | none | `{ email, token, newPassword }`. Tokens expire after 30 minutes and work once |

Register body: `{ email, password, firstName, lastName, phone? }`. Passwords must be 8 characters to 72 bytes. Emails are stored lower-cased.

**Password reset delivery:** there is no email sending yet. In the Development environment, `forgot-password` returns the token as `devToken` in the response. In any other environment it is never returned, so an email service must be added before real use.

### Users

| Method | Path | Auth | Description |
|---|---|---|---|
| PUT | `/api/users/{id}` | self or Admin | `{ email, firstName, lastName, phone?, role? }`. Only an Admin can change a role, and not their own |
| POST | `/api/users/{id}/change-password` | self or Admin | `{ currentPassword?, newPassword }`. `currentPassword` is required unless an Admin is changing someone else's |

Access errors return 403; unknown users return 404.

### Hotels

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/hotels` | none | Paged list. Filters: `city`, `country`, `page`, `pageSize` |
| GET | `/api/hotels/{id}` | none | One hotel. 404 if unknown |
| POST | `/api/hotels` | Admin | `{ name, addressLine1, city, country, postalCode? }`. 201 |
| PUT | `/api/hotels/{id}` | Admin | Same body. 200 |
| DELETE | `/api/hotels/{id}` | Admin | 204, or 409 if its rooms have reservations |

### Rooms

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/rooms` | none | Paged list of active rooms. Filters: `hotelId`, `roomType`, `minCapacity`, `maxPrice`, `page`, `pageSize`. `includeInactive=true` only takes effect for Staff/Admin |
| GET | `/api/rooms/{id}` | none | One room. Inactive rooms are 404 unless Staff/Admin |
| POST | `/api/rooms` | Staff/Admin | `{ hotelId, roomNumber, roomType, capacity, pricePerNight, isActive? }`. 201, 409 if the number exists in that hotel |
| PUT | `/api/rooms/{id}` | Staff/Admin | Same body. The hotel cannot be changed |
| DELETE | `/api/rooms/{id}` | Admin | 204, or 409 if it has reservations (deactivate it instead) |

List endpoints return `{ items, total, page, pageSize }`. `pageSize` defaults to 25 and is capped at 100.

### Roles

- **Customer:** manages their own account.
- **Staff:** reads all hotels, rooms, reservations and payments, and customer accounts (development table viewer for now).
- **Admin:** full access, including editing any user.

The token carries the user id (`sub`), email and role. Tokens stay valid until they expire, even after a password change.

## Development test page

The page lives outside the API project, in [test-client/index.html](../test-client/index.html), and talks to the API over HTTP like any real client. Serve it from any static server on an origin listed in `Cors:AllowedOrigins`:

```powershell
python -m http.server 5500 --directory ..\test-client
```

Then open http://localhost:5500/. The API base URL defaults to `http://localhost:5047`; override it with `?api=http://host:port`.

It lets you:
- log in and out, and see who is logged in
- register, edit profile, change password, request and complete a password reset
- view the database tables, filtered by the logged-in role

The tables are served by `GET /api/dev/tables`, which is also development-only:

| Caller | Sees |
|---|---|
| Anonymous | hotels, active rooms |
| Customer | the above, plus own user row, reservations and payments |
| Staff | all hotels, rooms, reservations, payments, and customer users (no password hashes) |
| Admin | everything, including password hashes |

These dev endpoints are not the real API. Production endpoints must apply the same role and ownership rules themselves.

## Calling the API from MAUI

An untested sketch of how the MAUI client could consume the auth endpoints, following the project's MVVM split: a service abstraction, a typed `HttpClient`, and a ViewModel that only knows the abstraction. It assumes the `CommunityToolkit.Mvvm` package. DTOs should live in the shared contracts project, and the JSON property names are camelCase.

**Contracts** (mirror the API responses):

```csharp
public record LoginRequest(string Email, string Password);
public record UserDto(int Id, string Email, string FirstName, string LastName,
                      string? Phone, string Role, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public record LoginResponse(string Token, DateTime ExpiresAtUtc, UserDto User);
```

**Service abstraction and implementation:**

```csharp
public interface IAuthService
{
    Task<UserDto?> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default);
    void Logout();
}

public class AuthService(HttpClient http) : IAuthService
{
    private const string TokenKey = "authToken";

    public async Task<UserDto?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password), ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return null; // wrong email or password

        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(ct))!;
        await SecureStorage.Default.SetAsync(TokenKey, login.Token);
        return login.User;
    }

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        // The auth handler below adds the Bearer header.
        var response = await http.GetAsync("api/auth/me", ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Logout(); // expired or invalid token
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(ct);
    }

    public void Logout() => SecureStorage.Default.Remove(TokenKey);
}
```

**Attach the token to every request:**

```csharp
public class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await SecureStorage.Default.GetAsync("authToken");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}
```

**Registration** in `MauiProgram.cs`:

```csharp
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddHttpClient<IAuthService, AuthService>(c => c.BaseAddress = new Uri("http://localhost:5047/"))
                .AddHttpMessageHandler<AuthHeaderHandler>();
builder.Services.AddTransient<LoginViewModel>();
builder.Services.AddTransient<LoginPage>();
```

**ViewModel:**

```csharp
public partial class LoginViewModel(IAuthService auth) : ObservableObject
{
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string? error;
    [ObservableProperty] private UserDto? currentUser;

    [RelayCommand]
    private async Task LoginAsync()
    {
        Error = null;
        try
        {
            CurrentUser = await auth.LoginAsync(Email, Password);
            if (CurrentUser is null)
                Error = "Invalid email or password.";
        }
        catch (HttpRequestException)
        {
            Error = "Could not reach the server.";
        }
    }
}
```

**View** (`LoginPage.xaml`, with `x:DataType="vm:LoginViewModel"`):

```xml
<VerticalStackLayout Padding="20" Spacing="10">
    <Entry Placeholder="Email" Keyboard="Email" Text="{Binding Email}" />
    <Entry Placeholder="Password" IsPassword="True" Text="{Binding Password}" />
    <Button Text="Log in" Command="{Binding LoginCommand}" />
    <Label Text="{Binding Error}" TextColor="Red" />
    <Label Text="{Binding CurrentUser.FirstName, StringFormat='Logged in as {0}'}" />
</VerticalStackLayout>
```

Notes for real use:

- **Plain HTTP is for local development only.** The target architecture uses HTTPS, which needs the .NET dev certificate trusted (`dotnet dev-certs https --trust`) and the `https` launch profile (`https://localhost:7170`).
- **Call `GetCurrentUserAsync()` at startup** to restore the session from the stored token. Tokens last 60 minutes, so handle a 401 anywhere by sending the user back to the login page.
- Other endpoints follow the same pattern. For example, an edit is `PutAsJsonAsync($"api/users/{id}", body)`, and 403 means the caller isn't allowed.

## Tips

- If `dotnet build` fails copying `HotelReservation.Api.exe`, a previous instance of the API is still running and locking the file. Stop it first.
- The API listens on `http://localhost:5047` (and `https://localhost:7170` with the `https` profile).

## Not done yet

- Reservation and payment endpoints (hotels and rooms are done)
- CORS policy for browser clients on other origins, and an OpenAPI document
- Containerizing the API (Dockerfile, adding it to Compose)
- Email delivery for password reset
- Automated tests
- Notifications table (optional in the schema doc)
