using System.Text.Json;
using MySqlConnector;

const string migrationId = "20251112083619_AddSeatLayoutAndSeatEntities";
const string productVersion = "9.0.10";

// locate appsettings.json by walking up the directory tree
string FindAppSettings()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, "appsettings.json");
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return string.Empty;
}

string settingsPath;
if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
{
    settingsPath = args[0];
    if (!File.Exists(settingsPath))
    {
        Console.Error.WriteLine($"Provided appsettings.json not found: {settingsPath}");
        return 1;
    }
}
else
{
    settingsPath = FindAppSettings();
    if (string.IsNullOrEmpty(settingsPath))
    {
        Console.Error.WriteLine("Could not find appsettings.json in parent directories.");
        return 1;
    }
}

var json = File.ReadAllText(settingsPath);
using var doc = JsonDocument.Parse(json);
if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) || !cs.TryGetProperty("DefaultConnection", out var connElem))
{
    Console.Error.WriteLine("Connection string not found in appsettings.json");
    return 1;
}
var connStr = connElem.GetString();
if (string.IsNullOrEmpty(connStr))
{
    Console.Error.WriteLine("Connection string is empty");
    return 1;
}

try
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "INSERT INTO `__efmigrationshistory` (`MigrationId`,`ProductVersion`) VALUES (@id,@ver);";
    cmd.Parameters.AddWithValue("@id", migrationId);
    cmd.Parameters.AddWithValue("@ver", productVersion);
    var affected = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Inserted migration record '{migrationId}' (rows affected: {affected}).");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error inserting migration record: " + ex.Message);
    return 2;
}
