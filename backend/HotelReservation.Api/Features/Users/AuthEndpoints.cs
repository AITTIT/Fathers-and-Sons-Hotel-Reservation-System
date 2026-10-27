using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservation.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Users;

public static class AuthEndpoints
{
    private const int MinPasswordLength = 8;

    // Used to keep login timing similar when the email does not exist.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("dummy-password");

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, ct))
                return Results.Conflict(new { error = "Email is already registered." });

            var now = DateTime.UtcNow;
            var user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                Role = "Customer",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Lost a race with a concurrent registration of the same email.
                return Results.Conflict(new { error = "Email is already registered." });
            }

            return Results.Created($"/api/users/{user.Id}", UserResponse.From(user));
        });

        group.MapPost("/login", async (LoginRequest request, AppDbContext db, TokenService tokens, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

            var valid = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);
            if (user is null || !valid)
                return Results.Json(new { error = "Invalid email or password." }, statusCode: StatusCodes.Status401Unauthorized);

            var (token, expires) = tokens.Create(user);
            return Results.Ok(new LoginResponse(token, expires, UserResponse.From(user)));
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id))
                return Results.Unauthorized();

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            return user is null ? Results.Unauthorized() : Results.Ok(UserResponse.From(user));
        }).RequireAuthorization();

        // Always answers the same way so it can't be used to discover which emails exist.
        // There is no email delivery yet: in Development the token is returned in the response.
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, AppDbContext db, IWebHostEnvironment env, CancellationToken ct) =>
        {
            const string message = "If the email is registered, a reset token has been issued.";
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null)
                return Results.Ok(new { message });

            var now = DateTime.UtcNow;
            var stale = await db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAtUtc == null)
                .ToListAsync(ct);
            foreach (var t in stale)
                t.UsedAtUtc = now;

            var rawToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(30)
            });
            await db.SaveChangesAsync(ct);

            return env.IsDevelopment()
                ? Results.Ok(new { message, devToken = rawToken })
                : Results.Ok(new { message });
        });

        group.MapPost("/reset-password", async (ResetPasswordRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (PasswordError(request.NewPassword) is { } passwordError)
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.NewPassword)] = [passwordError] });

            const string invalid = "Invalid or expired reset token.";
            var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            var hash = HashToken(request.Token ?? string.Empty);
            var now = DateTime.UtcNow;

            var entry = await db.PasswordResetTokens
                .Join(db.Users, t => t.UserId, u => u.Id, (t, u) => new { Token = t, User = u })
                .FirstOrDefaultAsync(x => x.Token.TokenHash == hash && x.User.Email == email, ct);

            if (entry is null || entry.Token.UsedAtUtc is not null || entry.Token.ExpiresAtUtc <= now)
                return Results.BadRequest(new { error = invalid });

            entry.Token.UsedAtUtc = now;
            entry.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
            entry.User.UpdatedAtUtc = now;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Password has been reset." });
        });
    }

    internal static string? PasswordError(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            return $"Password must be at least {MinPasswordLength} characters.";
        // bcrypt only uses the first 72 bytes
        if (System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            return "Password must be at most 72 bytes.";
        return null;
    }

    internal static string HashToken(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static Dictionary<string, string[]> Validate(RegisterRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains('@') || r.Email.Trim().Length > 254)
            errors[nameof(r.Email)] = ["A valid email is required."];
        if (PasswordError(r.Password) is { } passwordError)
            errors[nameof(r.Password)] = [passwordError];
        if (string.IsNullOrWhiteSpace(r.FirstName))
            errors[nameof(r.FirstName)] = ["First name is required."];
        if (string.IsNullOrWhiteSpace(r.LastName))
            errors[nameof(r.LastName)] = ["Last name is required."];

        return errors;
    }
}
