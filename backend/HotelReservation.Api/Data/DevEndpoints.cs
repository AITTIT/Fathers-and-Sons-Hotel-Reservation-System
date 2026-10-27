using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Data;

// Development-only helpers used by the test page in wwwroot.
public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dev");

        group.MapGet("/tables", async (AppDbContext db, CancellationToken ct) =>
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync(ct);
            try
            {
                var names = new List<string>();
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = """
                        SELECT table_name FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
                        ORDER BY table_name
                        """;
                    await using var reader = await cmd.ExecuteReaderAsync(ct);
                    while (await reader.ReadAsync(ct))
                        names.Add(reader.GetString(0));
                }

                var result = new List<object>();
                foreach (var name in names)
                {
                    await using var cmd = conn.CreateCommand();
                    // Name comes from information_schema; quote it defensively.
                    cmd.CommandText = $"SELECT * FROM \"{name.Replace("\"", "\"\"")}\" LIMIT 200";
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

                    result.Add(new { name, columns, rows });
                }

                return Results.Ok(result);
            }
            finally
            {
                await conn.CloseAsync();
            }
        });
    }
}
