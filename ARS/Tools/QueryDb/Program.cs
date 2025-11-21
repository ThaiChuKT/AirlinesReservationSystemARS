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

async Task RunQuery(string sql, bool showRows = false, int maxRows = 10)
{
    try
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = await cmd.ExecuteReaderAsync();
        var hasRows = false;
        int row = 0;
        while (await reader.ReadAsync())
        {
            hasRows = true;
            if (showRows)
            {
                // print first row columns
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                Console.WriteLine(string.Join(" | ", values));
                row++;
                if (row >= maxRows) break;
            }
            else
            {
                // print first column only
                Console.WriteLine(reader.GetValue(0));
            }
        }
        if (!hasRows)
            Console.WriteLine("(no results)");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Query failed: {ex.Message}");
    }
}

Console.WriteLine("Verifying FlightSeats table and mappings...");
Console.WriteLine("Check: does FlightSeats table exist?");
await RunQuery("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'FlightSeats';");

Console.WriteLine("\nTotal FlightSeats rows:");
await RunQuery("SELECT COUNT(*) FROM FlightSeats;");

Console.WriteLine("\nTotal FlightSeats reserved (Status='Reserved'):");
await RunQuery("SELECT COUNT(*) FROM FlightSeats WHERE Status = 'Reserved';");

Console.WriteLine("\nTotal Reservations with FlightSeatId set:");
await RunQuery("SELECT COUNT(*) FROM Reservations WHERE FlightSeatId IS NOT NULL;");

Console.WriteLine("\nSample FlightSeats rows (up to 10):");
await RunQuery("SELECT FlightSeatId, ScheduleId, SeatId, Status, ReservedByReservationID, Price FROM FlightSeats LIMIT 10;", showRows: true);

Console.WriteLine("\nSample Reservations that reference FlightSeats (up to 10):");
await RunQuery("SELECT ReservationID, FlightSeatId, SeatId, ScheduleID, ConfirmationNumber FROM Reservations WHERE FlightSeatId IS NOT NULL LIMIT 10;", showRows: true);

Console.WriteLine("\nSample FlightSeats that are reserved with their reservation details (up to 10):");
await RunQuery(@"SELECT fs.FlightSeatId, fs.ScheduleId, fs.SeatId, fs.Status, fs.ReservedByReservationID, r.ConfirmationNumber
FROM FlightSeats fs
LEFT JOIN Reservations r ON r.ReservationID = fs.ReservedByReservationID
WHERE fs.ReservedByReservationID IS NOT NULL
LIMIT 10;", showRows: true);

return 0;
