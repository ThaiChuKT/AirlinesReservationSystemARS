using MySqlConnector;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

string settingsPath = args.Length > 0 ? args[0] : Path.Combine("..", "appsettings.json");
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

// Find duplicate groups
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = @"SELECT SeatLayoutId, Label, GROUP_CONCAT(SeatId ORDER BY SeatId) AS ids, MIN(SeatId) AS keepId, COUNT(*) AS cnt
FROM Seats
GROUP BY SeatLayoutId, Label
HAVING COUNT(*) > 1;";
    using var reader = await cmd.ExecuteReaderAsync();
    var groups = new List<(int layout, string label, string ids, int keepId)>();
    while (await reader.ReadAsync())
    {
        groups.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
    }
    reader.Close();

    if (groups.Count == 0)
    {
        Console.WriteLine("No duplicate Seats found.");
        return 0;
    }

    Console.WriteLine($"Found {groups.Count} duplicate seat groups. Processing...");

    foreach (var g in groups)
    {
        var ids = g.ids.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).Select(int.Parse).ToList();
        var keep = g.keepId;
        var toReplace = ids.Where(i => i != keep).ToList();
        Console.WriteLine($"Processing SeatLayout {g.layout} label {g.label} keep={keep} replace=[{string.Join(',', toReplace)}]");

        // Handle FlightSeats that reference duplicate SeatIds. We need to merge where necessary
        if (toReplace.Count > 0)
        {
            // Load affected FlightSeats
            using var fsCmd = conn.CreateCommand();
            fsCmd.CommandText = $"SELECT FlightSeatId, ScheduleId, SeatId, ReservedByReservationID FROM FlightSeats WHERE SeatId IN ({string.Join(',', toReplace)})";
            using var fsReader = await fsCmd.ExecuteReaderAsync();
            var affectedFlightSeats = new List<(int fsId, int scheduleId, int seatId, int? reserved)>();
            while (await fsReader.ReadAsync())
            {
                affectedFlightSeats.Add((fsReader.GetInt32(0), fsReader.GetInt32(1), fsReader.GetInt32(2), fsReader.IsDBNull(3) ? (int?)null : fsReader.GetInt32(3)));
            }
            fsReader.Close();

            foreach (var afs in affectedFlightSeats)
            {
                // Check if a FlightSeat with the target keep SeatId already exists for this schedule
                using var existsCmd = conn.CreateCommand();
                existsCmd.CommandText = "SELECT FlightSeatId, ReservedByReservationID FROM FlightSeats WHERE ScheduleId = @sched AND SeatId = @keep LIMIT 1";
                existsCmd.Parameters.AddWithValue("@sched", afs.scheduleId);
                existsCmd.Parameters.AddWithValue("@keep", keep);
                using var existsReader = await existsCmd.ExecuteReaderAsync();
                if (await existsReader.ReadAsync())
                {
                    var keepFsId = existsReader.GetInt32(0);
                    var keepReserved = existsReader.IsDBNull(1) ? (int?)null : existsReader.GetInt32(1);
                    existsReader.Close();

                    // Merge reservation if needed
                    if (afs.reserved.HasValue)
                    {
                        if (!keepReserved.HasValue)
                        {
                            // Move reservation to keep FlightSeat
                            using var moveCmd = conn.CreateCommand();
                            moveCmd.CommandText = "UPDATE FlightSeats SET ReservedByReservationID = @res WHERE FlightSeatId = @keepFsId";
                            moveCmd.Parameters.AddWithValue("@res", afs.reserved.Value);
                            moveCmd.Parameters.AddWithValue("@keepFsId", keepFsId);
                            await moveCmd.ExecuteNonQueryAsync();

                            // Update Reservation to point to keep FlightSeat
                            using var updRes = conn.CreateCommand();
                            updRes.CommandText = "UPDATE Reservations SET FlightSeatId = @keepFsId WHERE ReservationID = @res";
                            updRes.Parameters.AddWithValue("@keepFsId", keepFsId);
                            updRes.Parameters.AddWithValue("@res", afs.reserved.Value);
                            await updRes.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            // Both flight seats reserved: clear the duplicate's reservation mapping to avoid conflicting reservations
                            using var clrRes = conn.CreateCommand();
                            clrRes.CommandText = "UPDATE Reservations SET FlightSeatId = NULL WHERE ReservationID = @res";
                            clrRes.Parameters.AddWithValue("@res", afs.reserved.Value);
                            await clrRes.ExecuteNonQueryAsync();
                        }
                    }

                    // Delete the duplicate FlightSeat row
                    using var delFs = conn.CreateCommand();
                    delFs.CommandText = "DELETE FROM FlightSeats WHERE FlightSeatId = @fsId";
                    delFs.Parameters.AddWithValue("@fsId", afs.fsId);
                    await delFs.ExecuteNonQueryAsync();
                    Console.WriteLine($"Deleted duplicate FlightSeat {afs.fsId} for schedule {afs.scheduleId}.");
                }
                else
                {
                    existsReader.Close();
                    // No conflict: safe to update this FlightSeat to use the keep SeatId
                    using var updFs = conn.CreateCommand();
                    updFs.CommandText = "UPDATE FlightSeats SET SeatId = @keep WHERE FlightSeatId = @fsId";
                    updFs.Parameters.AddWithValue("@keep", keep);
                    updFs.Parameters.AddWithValue("@fsId", afs.fsId);
                    await updFs.ExecuteNonQueryAsync();
                    Console.WriteLine($"Updated FlightSeat {afs.fsId} to canonical SeatId {keep}.");
                }
            }

            // Update reservations that reference duplicate SeatId directly (legacy SeatId column)
            using var upd = conn.CreateCommand();
            upd.CommandText = $"UPDATE Reservations SET SeatId = @keep WHERE SeatId IN ({string.Join(',', toReplace)})";
            upd.Parameters.AddWithValue("@keep", keep);
            var affected = await upd.ExecuteNonQueryAsync();
            Console.WriteLine($"Updated {affected} reservation rows to canonical SeatId {keep}.");

            // Delete duplicate seats
            using var del = conn.CreateCommand();
            del.CommandText = $"DELETE FROM Seats WHERE SeatId IN ({string.Join(',', toReplace)})";
            var delCount = await del.ExecuteNonQueryAsync();
            Console.WriteLine($"Deleted {delCount} duplicate seat rows.");
        }
    }
}

Console.WriteLine("Done.");
return 0;