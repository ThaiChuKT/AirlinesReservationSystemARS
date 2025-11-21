using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using MySqlConnector;

// Simple seed tool: reads connection string from ARS/appsettings.json and inserts flights+ schedules
// Usage: dotnet run --project ARS/Tools/SeedFlights/SeedFlights.csproj

string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.json");
appSettingsPath = Path.GetFullPath(appSettingsPath);
if (!File.Exists(appSettingsPath))
{
    Console.WriteLine($"Could not find appsettings.json at {appSettingsPath}");
    return;
}

using var fs = File.OpenRead(appSettingsPath);
using var doc = JsonDocument.Parse(fs);
if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings) || !connStrings.TryGetProperty("DefaultConnection", out var defaultConn))
{
    Console.WriteLine("DefaultConnection not found in appsettings.json");
    return;
}

var connStr = defaultConn.GetString();
if (string.IsNullOrEmpty(connStr)) { Console.WriteLine("Connection string empty"); return; }

Console.WriteLine("Using connection: " + connStr);

// Connect and gather city ids
using var conn = new MySqlConnection(connStr);
await conn.OpenAsync();

var cityIds = new List<int>();
using (var cmd = new MySqlCommand("SELECT CityID FROM Cities", conn))
using (var r = await cmd.ExecuteReaderAsync())
{
    while (await r.ReadAsync())
    {
        cityIds.Add(r.GetInt32(0));
    }
}

if (cityIds.Count < 2)
{
    Console.WriteLine("Not enough cities in the database to seed flights (need 2+). Aborting.");
    return;
}

var startDate = DateOnly.FromDateTime(DateTime.Now);
var endDate = new DateOnly(2025, 12, 31);
var rnd = new Random();

int flightsPerDay = 8; // you can adjust
var aircrafts = new[] { "A320", "B737", "B787", "A321" };
var seatMap = new Dictionary<string,int> { ["A320"] = 180, ["B737"] = 160, ["B787"] = 300, ["A321"] = 220 };

int totalInserted = 0;

for (var d = startDate; d <= endDate; d = d.AddDays(1))
{
    for (int i = 0; i < flightsPerDay; i++)
    {
        // pick two different random cities
        int originIdx = rnd.Next(cityIds.Count);
        int destIdx;
        do { destIdx = rnd.Next(cityIds.Count); } while (destIdx == originIdx);
        int origin = cityIds[originIdx];
        int dest = cityIds[destIdx];

        // departure time spaced through the day
        var depHour = 6 + i * Math.Max(1, (16 / Math.Max(1, flightsPerDay - 1)));
        var depMinute = rnd.Next(0, 60);
        var departure = d.ToDateTime(new TimeOnly(depHour % 24, depMinute));

        var duration = rnd.Next(60, 360); // 1h to 6h
        var arrival = departure.AddMinutes(duration);

        var aircraft = aircrafts[rnd.Next(aircrafts.Length)];
        var totalSeats = seatMap[aircraft];

        var baseFare = Math.Round((decimal)(50 + rnd.NextDouble() * 450), 2);

        var flightNumber = $"AR{d:yyMM}{i:D2}{rnd.Next(100,999)}";

        // Insert flight then schedule inside transaction
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            using var ins = new MySqlCommand(@"INSERT INTO `Flights` (FlightNumber, OriginCityID, DestinationCityID, DepartureTime, ArrivalTime, Duration, AircraftType, TotalSeats, BaseFare) VALUES (@fn, @oc, @dc, @dep, @arr, @dur, @atype, @seats, @fare); SELECT LAST_INSERT_ID();", conn, tx);
            ins.Parameters.AddWithValue("@fn", flightNumber);
            ins.Parameters.AddWithValue("@oc", origin);
            ins.Parameters.AddWithValue("@dc", dest);
            ins.Parameters.AddWithValue("@dep", departure);
            ins.Parameters.AddWithValue("@arr", arrival);
            ins.Parameters.AddWithValue("@dur", duration);
            ins.Parameters.AddWithValue("@atype", aircraft);
            ins.Parameters.AddWithValue("@seats", totalSeats);
            ins.Parameters.AddWithValue("@fare", baseFare);

            var res = await ins.ExecuteScalarAsync();
            var newFlightId = Convert.ToInt32(res);

            using var insSched = new MySqlCommand(@"INSERT INTO `Schedules` (FlightID, Date, Status) VALUES (@fid, @date, 'Scheduled');", conn, tx);
            insSched.Parameters.AddWithValue("@fid", newFlightId);
            insSched.Parameters.AddWithValue("@date", d);
            await insSched.ExecuteNonQueryAsync();

            await tx.CommitAsync();
            totalInserted++;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to insert flight: " + ex.Message);
            try { await tx.RollbackAsync(); } catch { }
        }
    }
}

Console.WriteLine($"Inserted {totalInserted} flights and schedules through {endDate}");
await conn.CloseAsync();
