using Hub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;

//Serilog config
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);



//Logger
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

//INFRA
    builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

const string CorrelationHeader= "X-Correlation-ID";

app.Use(async (context,next) =>
{
    var correlationId = context.Request.Headers[CorrelationHeader].ToString();
    if (string.IsNullOrWhiteSpace(correlationId))
        correlationId = Guid.NewGuid().ToString("N");

    context.Response.Headers[CorrelationHeader] = correlationId;

    using(LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});
//LOG MIDDLEWARE

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString());
        diag.Set("UserAgent",http.Request.Headers.UserAgent.ToString());
    };
});
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json","Hub API v1");
        options.RoutePrefix = "swagger";

    });
}



app.MapGet("/db-ping", async (Hub.Infrastructure.AppDbContext db) =>
{
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();
        return Results.Ok(new { postgres = true });
    } catch (Exception ex)
    {
        var root = ex;
        while (root.InnerException is not null) root = root.InnerException;

        return Results.Ok(new {
       postgres = false,
       error = ex.GetType().Name,
       message = ex.Message,
       innerError = ex.InnerException?.GetType().Name,
        innerMessage = ex.InnerException?.Message,
        rootError = root.GetType().Name,
        rootMessage = root.Message
    });
    }
}
);

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
    status = "ok",
    service = "webhook-transformation-hub",
    utc = DateTimeOffset.UtcNow
    });
});

app.Run();