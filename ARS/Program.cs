using Microsoft.EntityFrameworkCore;
using ARS.Data;
using System.IO;
using Microsoft.AspNetCore.Identity;
using ARS.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

// Load environment variables from a local .env file (if present) so we can keep secrets out of source control.
// We implement a tiny loader here to avoid requiring an external package.
void LoadDotEnv()
{
    try
    {
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (!File.Exists(envPath)) return;

        foreach (var raw in File.ReadAllLines(envPath))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            // support KEY=VALUE and export KEY=VALUE
            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line.Substring(7).Trim();
            }

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();

            // remove surrounding quotes if present
            if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
            {
                value = value.Substring(1, value.Length - 2);
            }

            Environment.SetEnvironmentVariable(key, value);
        }
    }
    catch
    {
        // swallow: env loading is best-effort
    }
}

// load before building the configuration
LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add DbContext with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

// Register Identity with integer keys and EF stores
builder.Services.AddIdentity<ARS.Models.User, IdentityRole<int>>(options =>
{
    // simple password policy for development; adjust for production
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

// Development-only: ensure an Admin role and a seeded admin user exist.
// This uses configuration or environment variables to get credentials, but
// will only run when the environment is Development to avoid accidental seeding in production.
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = services.GetRequiredService<UserManager<ARS.Models.User>>();
        var config = services.GetRequiredService<IConfiguration>();

        var adminEmail = config["ADMIN_EMAIL"] ?? "admin@example.com";
        var adminUserName = config["ADMIN_USERNAME"] ?? adminEmail;
        var adminPassword = config["ADMIN_PASSWORD"] ?? "Admin123!"; // dev default

        // Create Admin role if missing
        if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
        {
            var roleResult = roleManager.CreateAsync(new IdentityRole<int>("Admin")).GetAwaiter().GetResult();
        }

        // Create admin user if missing
        var existing = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
        if (existing == null)
        {
            var admin = new ARS.Models.User
            {
                UserName = adminUserName,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                Gender = 'O'
            };

            var createResult = userManager.CreateAsync(admin, adminPassword).GetAwaiter().GetResult();
            if (createResult.Succeeded)
            {
                userManager.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
            }
        }
        else
        {
            // ensure role membership
            if (!userManager.IsInRoleAsync(existing, "Admin").GetAwaiter().GetResult())
            {
                userManager.AddToRoleAsync(existing, "Admin").GetAwaiter().GetResult();
            }
        }
    }
    catch
    {
        // swallow: seeding is best-effort for development convenience
    }

    // Seed a default seat layout if none exists (development only)
    try
    {
        using var scope2 = app.Services.CreateScope();
        var services2 = scope2.ServiceProvider;
        var db = services2.GetRequiredService<ApplicationDbContext>();

        // Schema repair: ensure necessary columns/tables exist to tolerate partial migrations
        try
        {
            db.Database.ExecuteSqlRaw(@"ALTER TABLE `Flights` ADD COLUMN IF NOT EXISTS `SeatLayoutId` int NULL;");
        }
        catch { /* best-effort */ }

        try
        {
            db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `SeatLayouts` (
                `SeatLayoutId` int NOT NULL AUTO_INCREMENT,
                `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
                `MetadataJson` longtext CHARACTER SET utf8mb4 NULL,
                PRIMARY KEY (`SeatLayoutId`)
            ) CHARACTER SET=utf8mb4;");
        }
        catch { }

        try
        {
            db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `Seats` (
                `SeatId` int NOT NULL AUTO_INCREMENT,
                `SeatLayoutId` int NOT NULL,
                `RowNumber` int NOT NULL,
                `Column` varchar(5) CHARACTER SET utf8mb4 NOT NULL,
                `Label` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
                `CabinClass` int NOT NULL,
                `IsExitRow` tinyint(1) NOT NULL,
                `IsPremium` tinyint(1) NOT NULL,
                `PriceModifier` decimal(10,2) NULL,
                CONSTRAINT `PK_Seats` PRIMARY KEY (`SeatId`)
            ) CHARACTER SET=utf8mb4;");
        }
        catch { }

        // Ensure Reservations table has SeatId and SeatLabel columns (defensive)
        try
        {
            db.Database.ExecuteSqlRaw(@"ALTER TABLE `Reservations` ADD COLUMN IF NOT EXISTS `SeatId` int NULL;");
            db.Database.ExecuteSqlRaw(@"ALTER TABLE `Reservations` ADD COLUMN IF NOT EXISTS `SeatLabel` varchar(10) NULL;");
        }
        catch { }

        try
        {
            // create indexes if not present (best-effort)
            db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS `IX_Reservations_SeatId` ON `Reservations` (`SeatId`);");
        }
        catch { }

        try
        {
            // MySQL does not support ALTER ... ADD CONSTRAINT IF NOT EXISTS, so
            // check INFORMATION_SCHEMA first and only add the FK when it's missing.
            try
            {
                var conn = db.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE CONSTRAINT_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'Reservations'
                      AND CONSTRAINT_NAME = 'FK_Reservations_Seats_SeatId'
                      AND CONSTRAINT_TYPE = 'FOREIGN KEY'";
                var exists = Convert.ToInt32(cmd.ExecuteScalar() ?? 0) > 0;
                if (!exists)
                {
                    db.Database.ExecuteSqlRaw(@"ALTER TABLE `Reservations` ADD CONSTRAINT `FK_Reservations_Seats_SeatId` FOREIGN KEY (`SeatId`) REFERENCES `Seats` (`SeatId`) ON DELETE SET NULL;");
                }
            }
            catch
            {
                // Best-effort: if anything goes wrong checking/adding the FK, swallow the error
                // to avoid breaking developer startup. The proper migration should add the FK.
            }
        }
        catch { }

        if (!db.SeatLayouts.Any())
        {
            var layout = new SeatLayout { Name = "Default-6x40" };
            db.SeatLayouts.Add(layout);
            db.SaveChanges();

            var cols = new[] { "A", "B", "C", "D", "E", "F" };
            var seats = new List<Seat>();
            for (int r = 1; r <= 40; r++)
            {
                var cabin = (r <= 10) ? CabinClass.First : ((r <= 30) ? CabinClass.Business : CabinClass.Economy);
                foreach (var c in cols)
                {
                    seats.Add(new Seat
                    {
                        SeatLayoutId = layout.SeatLayoutId,
                        RowNumber = r,
                        Column = c,
                        Label = $"{r}{c}",
                        CabinClass = cabin,
                        IsExitRow = false,
                        IsPremium = false
                    });
                }
            }

            db.Seats.AddRange(seats);
            db.SaveChanges();

            // Assign to flights without a layout
            db.Database.ExecuteSqlRaw("UPDATE `Flights` SET `SeatLayoutId` = {0} WHERE `SeatLayoutId` IS NULL", layout.SeatLayoutId);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seat layout seeding failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
