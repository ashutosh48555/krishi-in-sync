using AgriSyncIndia.Data;
using AgriSyncIndia.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Neon writes DATABASE_URL / DATABASE_URL_UNPOOLED / NEON_* into .env.local (gitignored).
// A real environment variable always wins, so the same code runs on a host without the file.
var dotenv = DotEnv.Read(
    Path.Combine(builder.Environment.ContentRootPath, ".env.local"),
    Path.Combine(builder.Environment.ContentRootPath, ".env"));

string? NeonValue(string key)
{
    var fromEnvironment = Environment.GetEnvironmentVariable(key);
    if (!string.IsNullOrWhiteSpace(fromEnvironment)) return fromEnvironment;
    return dotenv.TryGetValue(key, out var fromFile) && !string.IsNullOrWhiteSpace(fromFile) ? fromFile : null;
}

// Neon's connection strings are URIs; Npgsql needs host=...;password=... . Convert only if the
// host has not already been configured explicitly.
var neonConfig = new Dictionary<string, string?>();
var databaseUrl = NeonValue("DATABASE_URL");
var directDatabaseUrl = NeonValue("DATABASE_URL_UNPOOLED") ?? databaseUrl;

if (builder.Configuration.GetConnectionString("DefaultConnection") is null && databaseUrl is not null)
    neonConfig["ConnectionStrings:DefaultConnection"] = PostgresConnection.FromEnvironmentValue(databaseUrl);
if (builder.Configuration.GetConnectionString("Migrations") is null && directDatabaseUrl is not null)
    neonConfig["ConnectionStrings:Migrations"] = PostgresConnection.FromEnvironmentValue(directDatabaseUrl);

builder.Configuration.AddInMemoryCollection(neonConfig);

// Public Neon endpoints for the browser (never the secret, never the database URL).
var neonPublic = NeonOptions.From(dotenv);
builder.Services.AddSingleton(neonPublic);

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Secure whenever the (forwarded) request is https
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Clerk (optional). Values come from env vars / user-secrets; nothing is stored in appsettings.json.
builder.Services.Configure<ClerkOptions>(builder.Configuration.GetSection("Clerk"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClerkOptions>>().Value);
builder.Services.AddSingleton<IClerkKeyProvider, ClerkJwksKeyProvider>();
builder.Services.AddSingleton<ClerkTokenValidator>();
builder.Services.AddHttpClient<UserProvisioning>(c => c.Timeout = TimeSpan.FromSeconds(8));

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Render's proxy address is not fixed; the app is only reachable through it.
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

// Register HttpClient for Open-Meteo REST API ingestion
builder.Services.AddHttpClient("OpenMeteoClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
    client.DefaultRequestHeaders.Add("User-Agent", "AgriSyncIndia-SmartFarm/1.0 (Indian Smart Agriculture Platform)");
});

// Register OpenMeteoService via Dependency Injection
builder.Services.AddScoped<IOpenMeteoService, OpenMeteoService>();
builder.Services.AddScoped<DashboardViewModelBuilder>();

// Postgres when a connection string is set, otherwise a local SQLite file.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
bool usePostgres = !string.IsNullOrWhiteSpace(connectionString);

builder.Services.AddDbContext<AgriSyncDbContext>(options =>
{
    if (usePostgres)
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3));
    else
        options.UseSqlite("Data Source=AgriSyncIndia.db");
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
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var clerkOrigin = app.Services.GetRequiredService<ClerkOptions>().Issuer;
var neonPublicOptions = app.Services.GetRequiredService<NeonOptions>();
var csp = SecurityHeaders.ContentSecurityPolicy(
    clerkOrigin,
    SecurityHeaders.Origin(neonPublicOptions.RealtimeUrl),
    SecurityHeaders.Origin(neonPublicOptions.TelemetryUrl));
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["Content-Security-Policy"] = csp;
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    h["X-Frame-Options"] = "DENY";
    await next();
});

app.UseStaticFiles();
app.UseMiddleware<LanguageMiddleware>();

app.UseRouting();
app.UseRateLimiter();
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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
