using System.Text.Json;
using MySqlConnector;

string? settingsPath = args.Length > 0 ? args[0] : Path.Combine("..", "appsettings.json");
if (!File.Exists(settingsPath))
{
    Console.Error.WriteLine($"appsettings.json not found at: {settingsPath}");
    return 1;
}

var json = File.ReadAllText(settingsPath);
using var doc = JsonDocument.Parse(json);
if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) || !cs.TryGetProperty("DefaultConnection", out var connElem))
{
    Console.Error.WriteLine("Connection string not found in appsettings.json");
    return 1;
}
var connStr = connElem.GetString()!;

using var conn = new MySqlConnection(connStr);
await conn.OpenAsync();

async Task RunScalar(string sql, string label)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    var val = await cmd.ExecuteScalarAsync();
    Console.WriteLine($"{label}: {val}");
}

async Task RunRows(string sql, string label)
{
    Console.WriteLine(label + ":");
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    using var reader = await cmd.ExecuteReaderAsync();
    var cnt = 0;
    while (await reader.ReadAsync())
    {
        var vals = new object[reader.FieldCount];
        reader.GetValues(vals);
        Console.WriteLine(string.Join(" | ", vals));
        cnt++;
        if (cnt >= 20) break;
    }
    if (cnt == 0) Console.WriteLine("(no rows)");
}

await RunScalar("SELECT COUNT(*) FROM Seats;", "Total Seats");
await RunScalar("SELECT COUNT(*) FROM (SELECT SeatLayoutId, Label, COUNT(*) c FROM Seats GROUP BY SeatLayoutId, Label HAVING c>1) x;", "Duplicate seat-label groups");
await RunRows("SELECT SeatLayoutId, Label, COUNT(*) FROM Seats GROUP BY SeatLayoutId, Label HAVING COUNT(*)>1 LIMIT 20;", "Sample duplicate seat groups");
await RunScalar("SELECT COUNT(*) FROM FlightSeats;", "Total FlightSeats");
await RunScalar("SELECT COUNT(*) FROM (SELECT ScheduleId, SeatId, COUNT(*) c FROM FlightSeats GROUP BY ScheduleId, SeatId HAVING c>1) x;", "Duplicate flightseat groups");
await RunRows("SELECT ScheduleId, SeatId, COUNT(*) FROM FlightSeats GROUP BY ScheduleId, SeatId HAVING COUNT(*)>1 LIMIT 20;", "Sample duplicate flightseat groups");
await RunRows("SELECT ScheduleId, COUNT(*) FROM FlightSeats GROUP BY ScheduleId LIMIT 20;", "FlightSeats per schedule (sample)");

return 0;