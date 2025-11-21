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

Console.WriteLine("Searching for duplicate FlightSeats (ScheduleId, SeatId)...");
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"SELECT ScheduleId, SeatId, COUNT(*) AS cnt FROM FlightSeats GROUP BY ScheduleId, SeatId HAVING COUNT(*)>1;";
    using var reader = await cmd.ExecuteReaderAsync();
    var duplicates = new List<(int schedule, int seat, int cnt)>();
    while (await reader.ReadAsync())
    {
        duplicates.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
    }
    reader.Close();

    if (duplicates.Count == 0)
    {
        Console.WriteLine("No duplicates found.");
        return 0;
    }

    Console.WriteLine($"Found {duplicates.Count} duplicate groups. Proceeding to remove duplicate rows (keeping smallest FlightSeatId) ...");

    // Delete duplicates: keep the smallest FlightSeatId for each (ScheduleId, SeatId)
    // MySQL deletion pattern: DELETE t1 FROM FlightSeats t1 JOIN FlightSeats t2 ON t1.ScheduleId = t2.ScheduleId AND t1.SeatId = t2.SeatId AND t1.FlightSeatId > t2.FlightSeatId;
    // This will remove all rows with higher id where a lower id exists.
    using var delCmd = conn.CreateCommand();
    delCmd.CommandText = @"DELETE t1 FROM FlightSeats t1 JOIN FlightSeats t2 ON t1.ScheduleId = t2.ScheduleId AND t1.SeatId = t2.SeatId AND t1.FlightSeatId > t2.FlightSeatId;";
    var affected = await delCmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Deleted {affected} duplicate rows.");

    // Verify no duplicates remain
    cmd.CommandText = @"SELECT COUNT(*) FROM (SELECT ScheduleId, SeatId FROM FlightSeats GROUP BY ScheduleId, SeatId HAVING COUNT(*)>1) x;";
    var remain = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
    Console.WriteLine($"Duplicate groups remaining: {remain}");
}

Console.WriteLine("Dedupe complete.");
return 0;
