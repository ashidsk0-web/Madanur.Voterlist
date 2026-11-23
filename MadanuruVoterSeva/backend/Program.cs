using Dapper;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using MadanuruVoterSeva.Backend.Data;
using MadanuruVoterSeva.Backend.Interfaces;
using MadanuruVoterSeva.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

if (args.Length > 0 && args[0] == "test-card")
{
    var pdfPath = args.Length > 1 ? args[1] : Path.GetFullPath(@"../data/source/MADANURU WITH SORTING VOTER.pdf");
    CardTester.TestCard(pdfPath);
    return;
}

if (args.Length > 0 && args[0] == "import-roll")
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    var factory = new SqliteConnectionFactory(config);
    using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    var initLogger = loggerFactory.CreateLogger<DbInitializer>();
    var initializer = new DbInitializer(factory, initLogger);
    initializer.Initialize();

    var logger = loggerFactory.CreateLogger<PdfExtractionService>();
    var service = new PdfExtractionService(factory, logger, config);
    Console.WriteLine("=== Extracting Electoral Roll from PDF ===");
    int rollId = service.ExtractAndStageRollAsync().GetAwaiter().GetResult();
    Console.WriteLine($"Staged Roll ID: {rollId}");
    var val = service.ValidateStagingAsync(rollId).GetAwaiter().GetResult();
    Console.WriteLine($"Validation: {val.ValidRecords} valid, {val.WarningsCount} warnings, {val.ErrorsCount} errors.");
    if (!val.CanApprove || !service.ApproveRollVersionAsync(rollId).GetAwaiter().GetResult())
        throw new InvalidOperationException("The staged roll did not pass validation.");
    Console.WriteLine("=== Roll Approved and Activated Successfully! ===");
    return;
}

if (args is ["configure-admin"])
{
    var password = Console.ReadLine();
    if (password is null || Encoding.UTF8.GetByteCount(password) is < 12 or > 72)
        throw new ArgumentException("Supply a password of 12 to 72 UTF-8 bytes on standard input.");

    var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
    var factory = new SqliteConnectionFactory(config);
    using var logger = LoggerFactory.Create(b => b.AddConsole());
    new DbInitializer(factory, logger.CreateLogger<DbInitializer>()).Initialize();
    using var connection = factory.CreateConnection();
    using var transaction = connection.BeginTransaction();
    connection.Execute("""
        UPDATE AppUser SET IsActive = 0 WHERE Username <> 'admin' AND IsActive = 1;
        INSERT INTO AppUser (Username, PasswordHash, Role, IsActive)
        VALUES ('admin', @Hash, 'Admin', 1)
        ON CONFLICT(Username) DO UPDATE SET PasswordHash = excluded.PasswordHash, Role = 'Admin', IsActive = 1;
        """, new { Hash = BCrypt.Net.BCrypt.HashPassword(password) }, transaction);
    transaction.Commit();
    Console.WriteLine("Administrator configured.");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Add Data Protection
builder.Services.AddDataProtection()
    .SetApplicationName("MadanuruVoterSeva");

// Add Database & Services
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddScoped<PdfExtractionService>();
builder.Services.AddScoped<VoterSearchService>();

// Add Controllers and JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// CORS configuration for offline local frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                  {
                      return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                  }
                  return false;
              })
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT Authentication configuration
var jwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
builder.Configuration["JwtSettings:Secret"] = jwtSecret;
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Method == "GET" &&
                context.Request.Path.StartsWithSegments("/api/voters") &&
                context.Request.Path.Value?.EndsWith("/source/pdf", StringComparison.Ordinal) == true)
                context.Token = context.Request.Cookies["MadanuruDesk"];
            return Task.CompletedTask;
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});
builder.Services.AddOpenApi();

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var publicHost = builder.Configuration["Hosting:PublicHost"]?.Trim();
if (!string.IsNullOrEmpty(publicHost) && publicHost != "*" && Uri.CheckHostName(publicHost) != UriHostNameType.Dns)
    throw new InvalidOperationException("Hosting:PublicHost must be a DNS hostname without a scheme or port, or '*'.");

var app = builder.Build();

app.UseForwardedHeaders();

// Run SQLite Database Schema initialization and migrations
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    dbInitializer.Initialize();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    bool hosted = !string.IsNullOrEmpty(publicHost);
    bool hostAllowed;
    if (hosted)
    {
        if (publicHost == "*")
        {
            hostAllowed = true;
        }
        else
        {
            var host = context.Request.Host.Host;
            hostAllowed = string.Equals(host, publicHost, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(host, "www." + publicHost, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase);
        }
    }
    else
    {
        hostAllowed = Uri.TryCreate($"http://{context.Request.Host}", UriKind.Absolute, out var caller) && caller.IsLoopback;
    }

    if (!hostAllowed)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";
    if (context.Request.Path.StartsWithSegments("/voter-photos"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});
if (string.IsNullOrEmpty(publicHost) || publicHost == "*") app.UseCors("AllowLocalFrontend");
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Serve React Frontend static assets if available
var frontendDist = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(frontendDist) || !File.Exists(Path.Combine(frontendDist, "index.html")))
{
    var devDist = Path.GetFullPath("../frontend/dist", app.Environment.ContentRootPath);
    if (Directory.Exists(devDist) && File.Exists(Path.Combine(devDist, "index.html")))
    {
        frontendDist = devDist;
    }
}

if (Directory.Exists(frontendDist))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendDist);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider
    });
}

app.MapControllers();
app.MapFallback("/api/{**path}", () => Results.NotFound());

// Health and DB connection verification endpoint
app.MapGet("/api/health", (IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var connection = dbFactory.CreateConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM AppUser;";
        cmd.ExecuteScalar();
        return Results.Ok(new
        {
            status = "Healthy",
            database = "Connected",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database health check failed.");
        return Results.Problem("Local database connection failed.");
    }
});

if (Directory.Exists(frontendDist))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendDist);
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = fileProvider
    });
}

app.Run();
