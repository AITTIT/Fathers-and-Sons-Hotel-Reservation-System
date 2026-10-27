using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HotelReservation.Api.Data;

// Development-only helpers used by the test page in wwwroot.
public static class DevEndpoints
{
    private const string UserColumns =
        "\"Id\", \"Email\", \"FirstName\", \"LastName\", \"Phone\", \"Role\", \"CreatedAtUtc\", \"UpdatedAtUtc\"";

    private record TableQuery(string Name, string Sql, bool NeedsUserId = false);

    public static void MapDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dev");

        // Visibility depends on the caller's role; anonymous callers get public data only.
        group.MapGet("/tables", async (ClaimsPrincipal principal, AppDbContext db, CancellationToken ct) =>
        {
            int? userId = int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
            var role = userId is null ? null : principal.FindFirstValue(ClaimTypes.Role);

            var conn = (NpgsqlConnection)db.Database.GetDbConnection();
            await conn.OpenAsync(ct);
            try
            {
                var result = new List<object>();
                foreach (var query in QueriesFor(role, userId))
                {
                    await using var cmd = new NpgsqlCommand(query.Sql, conn);
                    if (query.NeedsUserId)
                        cmd.Parameters.AddWithValue("userId", userId!.Value);
                    await using var reader = await cmd.ExecuteReaderAsync(ct);

                    var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    var rows = new List<object?[]>();
                    while (await reader.ReadAsync(ct))
                    {
                        var row = new object?[reader.FieldCount];
                        for (var i = 0; i < row.Length; i++)
                            row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        rows.Add(row);
                    }

                    result.Add(new { name = query.Name, columns, rows });
                }

                return Results.Ok(result);
            }
            finally
            {
                await conn.CloseAsync();
            }
        });
    }

    private static List<TableQuery> QueriesFor(string? role, int? userId)
    {
        switch (role)
        {
            case "Admin":
                return
                [
                    new("Users", "SELECT * FROM \"Users\" ORDER BY \"Id\" LIMIT 200"),
                    new("Hotels", "SELECT * FROM \"Hotels\" ORDER BY \"Id\" LIMIT 200"),
                    new("Rooms", "SELECT * FROM \"Rooms\" ORDER BY \"Id\" LIMIT 200"),
                    new("Reservations", "SELECT * FROM \"Reservations\" ORDER BY \"Id\" LIMIT 200")
                ];

            case "Staff":
                return
                [
                    new("Users",
                        $"SELECT {UserColumns} FROM \"Users\" WHERE \"Role\" = 'Customer' OR \"Id\" = @userId ORDER BY \"Id\" LIMIT 200",
                        NeedsUserId: true),
                    new("Hotels", "SELECT * FROM \"Hotels\" ORDER BY \"Id\" LIMIT 200"),
                    new("Rooms", "SELECT * FROM \"Rooms\" ORDER BY \"Id\" LIMIT 200"),
                    new("Reservations", "SELECT * FROM \"Reservations\" ORDER BY \"Id\" LIMIT 200")
                ];

            default:
                var queries = new List<TableQuery>();
                if (userId is not null)
                {
                    queries.Add(new("Users",
                        $"SELECT {UserColumns} FROM \"Users\" WHERE \"Id\" = @userId",
                        NeedsUserId: true));
                }

                queries.Add(new("Hotels", "SELECT * FROM \"Hotels\" ORDER BY \"Id\" LIMIT 200"));
                queries.Add(new("Rooms", "SELECT * FROM \"Rooms\" WHERE \"IsActive\" ORDER BY \"Id\" LIMIT 200"));

                if (userId is not null)
                {
                    queries.Add(new("Reservations",
                        "SELECT * FROM \"Reservations\" WHERE \"UserId\" = @userId ORDER BY \"Id\" LIMIT 200",
                        NeedsUserId: true));
                }

                return queries;
        }
    }
}
