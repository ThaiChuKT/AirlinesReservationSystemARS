using Microsoft.EntityFrameworkCore;
using ARS.Data;
using System.IO;
using Microsoft.AspNetCore.Identity;
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
