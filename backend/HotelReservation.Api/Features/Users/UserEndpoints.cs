using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelReservation.Api.Data;
using HotelReservation.Api.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Features.Users;

public static class UserEndpoints
{
    private static readonly string[] Roles = ["Customer", "Staff", "Admin"];

    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        // Staff only see customer accounts; admins see everyone.
        group.MapGet("/", async (string? role, string? search, int? page, int? pageSize, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            if (!caller.IsInRole("Staff") && !caller.IsInRole("Admin"))
                return Results.Forbid();
            if (role is not null && !Roles.Contains(role))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["role"] = [$"Role must be one of: {string.Join(", ", Roles)}."] });

            var query = db.Users.AsNoTracking().AsQueryable();
            if (!caller.IsInRole("Admin"))
                query = query.Where(u => u.Role == "Customer");
            if (role is not null)
                query = query.Where(u => u.Role == role);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u => u.Email.Contains(term) || u.FirstName.ToLower().Contains(term) || u.LastName.ToLower().Contains(term));
            }

            var result = await query.OrderBy(u => u.Id).Select(u => UserResponse.From(u)).ToPagedAsync(page, pageSize, ct);
            return Results.Ok(result);
        });

        // Users can edit themselves; admins can edit anyone and are the only ones who can change roles.
        group.MapPut("/{id:int}", async (int id, UpdateUserRequest request, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            if (!CanAccess(caller, id, out var isAdmin))
                return Results.Forbid();

            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@') || request.Email.Trim().Length > 254)
                errors[nameof(request.Email)] = ["A valid email is required."];
            if (string.IsNullOrWhiteSpace(request.FirstName))
                errors[nameof(request.FirstName)] = ["First name is required."];
            if (string.IsNullOrWhiteSpace(request.LastName))
                errors[nameof(request.LastName)] = ["Last name is required."];
            if (request.Role is not null && !Roles.Contains(request.Role))
                errors[nameof(request.Role)] = [$"Role must be one of: {string.Join(", ", Roles)}."];
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
                return Results.NotFound();

            if (request.Role is not null && request.Role != user.Role)
            {
                if (!isAdmin)
                    return Results.Forbid();
                // Stops an admin from locking everyone out of administration by accident.
                if (CallerId(caller) == id)
                    return Results.BadRequest(new { error = "Admins cannot change their own role." });
                user.Role = request.Role;
            }

            var email = request.Email.Trim().ToLowerInvariant();
            if (email != user.Email && await db.Users.AnyAsync(u => u.Email == email, ct))
                return Results.Conflict(new { error = "Email is already registered." });

            user.Email = email;
            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
            user.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { error = "Email is already registered." });
            }

            return Results.Ok(UserResponse.From(user));
        });

        group.MapPost("/{id:int}/change-password", async (int id, ChangePasswordRequest request, ClaimsPrincipal caller, AppDbContext db, CancellationToken ct) =>
        {
            if (!CanAccess(caller, id, out var isAdmin))
                return Results.Forbid();

            if (AuthEndpoints.PasswordError(request.NewPassword) is { } passwordError)
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.NewPassword)] = [passwordError] });

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
                return Results.NotFound();

            // Admins resetting someone else's password don't need to know the old one.
            var isSelf = CallerId(caller) == id;
            if (isSelf || !isAdmin)
            {
                if (string.IsNullOrEmpty(request.CurrentPassword) ||
                    !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                    return Results.BadRequest(new { error = "Current password is incorrect." });
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 11);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { message = "Password changed." });
        });
    }

    private static int? CallerId(ClaimsPrincipal caller) =>
        int.TryParse(caller.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    private static bool CanAccess(ClaimsPrincipal caller, int targetId, out bool isAdmin)
    {
        isAdmin = caller.IsInRole("Admin");
        return isAdmin || CallerId(caller) == targetId;
    }
}
