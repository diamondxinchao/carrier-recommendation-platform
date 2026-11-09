using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Carrier.Application;
using Carrier.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ICarrierReadRepository, EfCarrierReadRepository>();
builder.Services.AddScoped<CarrierDiscovery>();
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
if (provider is not ("Sqlite" or "SqlServer")) throw new InvalidOperationException("Unknown database provider.");
if (!builder.Environment.IsDevelopment() && provider == "Sqlite")
    throw new InvalidOperationException("SQLite is for development. Configure SQL Server for deployment.");
builder.Services.AddDbContext<CarrierDbContext>(o =>
{
    var connection = builder.Configuration.GetConnectionString("Carriers")
        ?? throw new InvalidOperationException("ConnectionStrings:Carriers is required.");
    if (provider == "SqlServer") o.UseSqlServer(connection, sql => sql.EnableRetryOnFailure());
    else o.UseSqlite(connection);
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .WithMethods("GET", "POST").AllowAnyHeader().WithExposedHeaders("Server-Timing")));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("discovery", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await Results.Problem(statusCode: 429, title: "Too many requests. Try again shortly.")
            .ExecuteAsync(context.HttpContext);
    };
});

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    var started = Stopwatch.GetTimestamp();
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Server-Timing"] = "api;dur=" + Stopwatch.GetElapsedTime(started).TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture);
        return Task.CompletedTask;
    });
    try { await next(context); }
    finally
    {
        app.Logger.LogInformation("HTTP {Method} {Path} {StatusCode} {ElapsedMs:F2}ms TraceId={TraceId}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.TraceIdentifier);
    }
});
app.UseCors();
app.UseRateLimiter();

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (CarrierDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.MapGet("/api/reference", () => Results.Ok(new
{
    cities = DemoData.Cities,
    equipment = new[] { "dryVan", "refrigerated", "flatbed" },
    dataNotice = "Fictional demo carriers. Rates and performance are illustrative; availability is not a booking guarantee."
}));

var api = app.MapGroup("/api").RequireRateLimiting("discovery");
api.MapGet("/carriers", async ([AsParameters] CarrierSearch search, CarrierDiscovery discovery,
    IConfiguration config, CancellationToken ct) => Results.Ok(await discovery.ExecuteAsync(search, false,
        config.GetValue("Features:RankingEnabled", true), ct)));
api.MapPost("/recommendations", async (CarrierSearch search, CarrierDiscovery discovery,
    IConfiguration config, CancellationToken ct) => Results.Ok(await discovery.ExecuteAsync(search, true,
        config.GetValue("Features:RankingEnabled", true), ct)));
api.MapGet("/carriers/{id:guid}", async (Guid id, CarrierDiscovery discovery, CancellationToken ct) =>
    await discovery.FindAsync(id, ct) is { } carrier ? Results.Ok(carrier) : Results.NotFound());

// Only development instances may create a schema and seed fictional records automatically.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:Initialize", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CarrierDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoData.SeedAsync(db, DateOnly.FromDateTime(DateTime.UtcNow));
}
app.Run();

public partial class Program { }

internal sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is SearchValidationException validation)
        {
            context.Response.StatusCode = 400;
            await Results.ValidationProblem(validation.Errors).ExecuteAsync(context);
            return true;
        }
        logger.LogError(exception, "Unhandled request error. TraceId={TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = 500;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new() { Status = 500, Title = "An unexpected error occurred.", Extensions = { ["traceId"] = context.TraceIdentifier } }
        });
    }
}
