using Dapper;
using System.Security.Cryptography;
using System.Text;
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

if (args.Length == 2 && args[0] == "reset-password")
{
    var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
    using var connection = new SqliteConnectionFactory(config).CreateConnection();
    var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
    var affected = connection.Execute(
        "UPDATE AppUser SET PasswordHash = @Hash WHERE Username = @User AND IsActive = 1;",
        new { Hash = BCrypt.Net.BCrypt.HashPassword(password), User = args[1] });
    if (affected != 1) throw new InvalidOperationException("Active account not found.");
    Console.WriteLine($"New password for {args[1]}: {password}");
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
builder.Services.AddOpenApi();

var app = builder.Build();

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
    if (!Uri.TryCreate($"http://{context.Request.Host}", UriKind.Absolute, out var caller) || !caller.IsLoopback)
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
app.UseCors("AllowLocalFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Serve React Frontend static assets if available
var frontendDist = Path.GetFullPath("../frontend/dist", app.Environment.ContentRootPath);

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
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return Results.Ok(new
        {
            status = "Healthy",
            database = "Connected",
            usersCount = count,
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
