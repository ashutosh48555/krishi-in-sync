using AgriSyncIndia.Data;
using AgriSyncIndia.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Cookie Authentication for Sign In / Sign Up
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AgriSyncAuthCookie";
        options.Cookie.HttpOnly = true;
    });

// Register HttpClient for Open-Meteo REST API ingestion
builder.Services.AddHttpClient("OpenMeteoClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.Add("User-Agent", "AgriSyncIndia-SmartFarm/1.0 (Indian Smart Agriculture Platform)");
});

// Register OpenMeteoService via Dependency Injection
builder.Services.AddScoped<IOpenMeteoService, OpenMeteoService>();

// Postgres when a connection string is set, otherwise a local SQLite file.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
bool usePostgres = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddDbContext<AgriSyncDbContext>(options =>
{
    if (usePostgres) options.UseNpgsql(connectionString);
    else options.UseSqlite("Data Source=AgriSyncIndia.db");
});

var dataProtection = builder.Services.AddDataProtection().SetApplicationName("AgriSyncIndia");
if (usePostgres)
{
    dataProtection.PersistKeysToDbContext<AgriSyncDbContext>();
}
else
{
    var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
    Directory.CreateDirectory(keysFolder);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysFolder));
}

var app = builder.Build();

// Create schema, then seed. Failures are fatal on purpose: a broken seed must stop the process.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AgriSyncDbContext>();
    if (usePostgres)
    {
        var migrationsConnection = builder.Configuration.GetConnectionString("Migrations") ?? connectionString;
        using var migrationContext = new AgriSyncDbContext(
            new DbContextOptionsBuilder<AgriSyncDbContext>().UseNpgsql(migrationsConnection).Options);
        await migrationContext.Database.MigrateAsync();
    }
    else
    {
        await context.Database.EnsureCreatedAsync();
    }
    await DbInitializer.InitializeAsync(context, services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Climate/Dashboard");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", async (AgriSyncDbContext db) =>
{
    var since = IstClock.Now.AddHours(-24);
    var logs24h = await db.SensorLogs.CountAsync(l => l.TimestampIST >= since);
    var latest = await db.SensorLogs.MaxAsync(l => (DateTime?)l.TimestampIST);
    var body = new
    {
        zones = await db.Zones.CountAsync(),
        sensorLogs24h = logs24h,
        rules = await db.AutomationRules.CountAsync(),
        alerts = await db.ClimateAlerts.CountAsync(),
        users = await db.Users.CountAsync(),
        latestLogIst = latest
    };
    return Results.Json(body, statusCode: logs24h == 0 ? 503 : 200);
}).AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Climate}/{action=Dashboard}/{id?}");

app.Run();
