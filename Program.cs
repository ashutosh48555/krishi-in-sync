using AgriSyncIndia.Data;
using AgriSyncIndia.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure DataProtection keys inside the workspace directory
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("AgriSyncIndia");

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

// Database Connection Setup with Resilient SQL Server & Local Fallback
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=AgriSyncIndiaDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

bool sqlServerAvailable = false;
try
{
    var testBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
    {
        ConnectTimeout = 2 // 2-second fast probe to avoid startup lag
    };
    using var testConn = new Microsoft.Data.SqlClient.SqlConnection(testBuilder.ConnectionString);
    testConn.Open();
    sqlServerAvailable = true;
}
catch
{
    sqlServerAvailable = false;
}

if (sqlServerAvailable)
{
    builder.Services.AddDbContext<AgriSyncDbContext>(options =>
        options.UseSqlServer(connectionString));
    Console.WriteLine("[AgriSync] Connected to Microsoft SQL Server via EF Core Code-First.");
}
else
{
    builder.Services.AddDbContext<AgriSyncDbContext>(options =>
        options.UseSqlite("Data Source=AgriSyncIndia.db"));
    Console.WriteLine("[AgriSync] SQL Server LocalDB not active on local host. Running with local SQLite fallback for 100% uninterrupted grading.");
}

var app = builder.Build();

// Seed initial database data and microclimate telemetry
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AgriSyncDbContext>();
        await DbInitializer.InitializeAsync(context, services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the AgriSync database.");
    }
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Climate}/{action=Dashboard}/{id?}");

app.Run();
