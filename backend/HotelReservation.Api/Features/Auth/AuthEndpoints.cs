using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using HotelReservation.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Auth;

public static class AuthEndpoints
{
    // Used to keep login timing similar when the email does not exist.
    private static readonly string DummyHash = PasswordRules.Hash("dummy-password");

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

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
                PasswordHash = PasswordRules.Hash(request.Password),
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
        }).RequireRateLimiting(Policies.AuthRateLimit)
        .WithSummary("Register a customer account")
        .WithDescription("Public. Always creates a Customer. 409 if the email is taken. Rate limited per IP.");

        group.MapPost("/login", async (LoginRequest request, AppDbContext db, TokenService tokens, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest(new { error = "Email and password are required." });

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

            var valid = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);
            if (user is null || !valid)
                return Results.Json(new { error = "Invalid email or password." }, statusCode: StatusCodes.Status401Unauthorized);

            return Results.Ok(await StartSessionAsync(user, db, tokens, ct));
        }).RequireRateLimiting(Policies.AuthRateLimit)
        .WithSummary("Log in and get tokens")
        .WithDescription("Public. Send the returned `token` as `Authorization: Bearer <token>`. It is short-lived; use `refreshToken` with `/api/auth/refresh` to get a new one. Rate limited per IP.");

        group.MapPost("/refresh", async (RefreshRequest request, AppDbContext db, TokenService tokens, CancellationToken ct) =>
        {
            var hash = HashToken(request.RefreshToken ?? string.Empty);
            var now = DateTime.UtcNow;
            var stored = await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
            if (stored is null || stored.RevokedAtUtc is not null || stored.ExpiresAtUtc <= now)
                return InvalidRefreshToken();

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            // Atomic, so of two simultaneous requests only one can win.
            var claimed = await db.RefreshTokens
                .Where(t => t.Id == stored.Id && t.UsedAtUtc == null && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, now), ct);
            if (claimed == 0)
            {
                // A token can only be used once, so someone else may have a copy. End all of the user's sessions.
                await RefreshTokens.RevokeAllAsync(db, stored.UserId, now, ct);
                await tx.CommitAsync(ct);
                return InvalidRefreshToken();
            }

            var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == stored.UserId, ct);
            var session = await StartSessionAsync(user, db, tokens, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(session);
        }).RequireRateLimiting(Policies.AuthRateLimit)
        .WithSummary("Exchange a refresh token for new tokens")
        .WithDescription("Public. Returns a new `token` and a new `refreshToken`, with the user's current role. Each refresh token works once: reusing one ends all of the user's sessions, so clients must never send two refreshes at once. Rate limited per IP.");

        // Possession of the token is enough, because the access token may already have expired.
        group.MapPost("/logout", async (LogoutRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var hash = HashToken(request.RefreshToken ?? string.Empty);
            await db.RefreshTokens
                .Where(t => t.TokenHash == hash && t.UsedAtUtc == null && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), ct);
            return Results.NoContent();
        })
        .WithSummary("Log out")
        .WithDescription("Public. Revokes the refresh token, so this client must log in again. Always 204, even if the token is unknown or already invalid. The current access token keeps working until it expires.");

        group.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id))
                return Results.Unauthorized();

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            return user is null ? Results.Unauthorized() : Results.Ok(UserResponse.From(user));
        }).RequireAuthorization()
        .WithSummary("Current user")
        .WithDescription("Requires login.");

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

            var rawToken = NewOpaqueToken();
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
        }).RequireRateLimiting(Policies.AuthRateLimit)
        .WithSummary("Request a password reset token")
        .WithDescription("Public. Always answers the same whether or not the email exists. There is no email delivery yet; in Development the token is returned as `devToken`. Rate limited per IP.");

        group.MapPost("/reset-password", async (ResetPasswordRequest request, AppDbContext db, CancellationToken ct) =>
        {
            if (PasswordRules.Error(request.NewPassword) is { } passwordError)
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
            entry.User.PasswordHash = PasswordRules.Hash(request.NewPassword);
            entry.User.UpdatedAtUtc = now;
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.SaveChangesAsync(ct);
            await RefreshTokens.RevokeAllAsync(db, entry.User.Id, now, ct);
            await tx.CommitAsync(ct);

            return Results.Ok(new { message = "Password has been reset." });
        }).RequireRateLimiting(Policies.AuthRateLimit)
        .WithSummary("Set a new password with a reset token")
        .WithDescription("Public. Tokens are single use and expire after 30 minutes. Ends all of the user's sessions once their access tokens expire. Rate limited per IP.");
    }

    private static IResult InvalidRefreshToken() =>
        Results.Json(new { error = "Invalid or expired refresh token." }, statusCode: StatusCodes.Status401Unauthorized);

    private static async Task<LoginResponse> StartSessionAsync(User user, AppDbContext db, TokenService tokens, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        // Keeps the table from growing forever; expired tokens can't do anything any more.
        await db.RefreshTokens.Where(t => t.UserId == user.Id && t.ExpiresAtUtc <= now).ExecuteDeleteAsync(ct);

        var (token, expires) = tokens.Create(user);
        var rawRefreshToken = NewOpaqueToken();
        var refreshExpires = tokens.RefreshTokenExpiry(now);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawRefreshToken),
            CreatedAtUtc = now,
            ExpiresAtUtc = refreshExpires
        });
        await db.SaveChangesAsync(ct);
        return new LoginResponse(token, expires, rawRefreshToken, refreshExpires, UserResponse.From(user));
    }

    private static string NewOpaqueToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string HashToken(string token) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static Dictionary<string, string[]> Validate(RegisterRequest r)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(r.Email) || !r.Email.Contains('@') || r.Email.Trim().Length > 254)
            errors[nameof(r.Email)] = ["A valid email is required."];
        if (PasswordRules.Error(r.Password) is { } passwordError)
            errors[nameof(r.Password)] = [passwordError];
        if (string.IsNullOrWhiteSpace(r.FirstName))
            errors[nameof(r.FirstName)] = ["First name is required."];
        if (string.IsNullOrWhiteSpace(r.LastName))
            errors[nameof(r.LastName)] = ["Last name is required."];

        return errors;
    }
}
