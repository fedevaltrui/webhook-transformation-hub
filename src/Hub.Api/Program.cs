using Hub.Domain.Entities;
using Hub.Infrastructure;
using Hub.Infrastructure.Security;
using Hub.Api.Security;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using System.Text.Json.Serialization;
using Hub.Api.Ingest;
using Hub.Api.Admin;





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
builder.Services.AddScoped<Hub.Api.Security.RequestAuthContext>();
builder.Services.AddScoped<Hub.Api.Security.ApiKeyAuthMiddleware>();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

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

//ApiKeyAuthMiddleware
app.UseMiddleware<Hub.Api.Security.ApiKeyAuthMiddleware>();

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

//OpenApi/Swagger
if (app.Environment.IsDevelopment())
{
    
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json","Hub API v1");
        options.RoutePrefix = "swagger";

    });

    app.MapPost("/admin/bootstrap", async (AppDbContext db, ApiKeyService keys, IConfiguration cfg, HttpRequest req) =>
    {
        var token = req.Headers["X-Bootstrap-Token"].ToString();
        var expected = cfg.GetSection("Security")["BootstrapToken"];

        if (string.IsNullOrWhiteSpace(expected) || token != expected)
            return Results.Unauthorized();

        var ws = await db.Workspaces.FirstOrDefaultAsync(x => x.Name == "Default");
        if (ws is null)
        {
            ws = new Workspace { Name = "Default" };
            db.Workspaces.Add(ws);
            await db.SaveChangesAsync();
        }

        var (row, plaintext) = await keys.CreateAsync(
            ws.Id,
            "bootstrap-admin",
            ApiKeyScopes.Admin | ApiKeyScopes.Read,
            expiresAtUtc: null);

        return Results.Ok(new
        {
            workspaceId = ws.Id,
            apiKeyId = row.Id,
            apiKey = plaintext,
            scopes = row.Scopes.ToString()
        });
    })
    .WithTags("Admin")
    .WithOpenApi(op =>
    {
        op.Summary = "Bootstrap a default workspace and admin API key (dev only).";
        op.Description = "Creates the Default workspace if needed and returns a one-time admin API key.";
        return op;
    });
}

//ENDPOINTS

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
)
.WithTags("System")
.WithOpenApi(op =>
{
    op.Summary = "Database connectivity check.";
    op.Description = "Attempts to open and close a PostgreSQL connection and returns status info.";
    return op;
});

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
    status = "ok",
    service = "webhook-transformation-hub",
    utc = DateTimeOffset.UtcNow
    });
})
.WithTags("System")
.WithOpenApi(op =>
{
    op.Summary = "Service health check.";
    op.Description = "Returns service status and current UTC timestamp.";
    return op;
});

 

    

var admin = app.MapGroup("/admin").RequireScopes(ApiKeyScopes.Admin).WithTags("Admin");

admin.MapPost("/workspaces", async (AppDbContext db, CreateWorkspaceRequest body) =>
{
    var ws = new Workspace { Name = body.Name.Trim() };
    db.Workspaces.Add(ws);
    await db.SaveChangesAsync();
    return Results.Ok(new {id = ws.Id, ws.Name, ws.CreatedAtUtc});
})
.WithOpenApi(op =>
{
    op.Summary = "Create a workspace.";
    op.Description = "Creates a new workspace for multi-tenant isolation.";
    return op;
});

admin.MapPost("/apikeys", async (ApiKeyService keys, CreateApiKeyRequest body) =>
{
    var (row, plaintext) = await keys.CreateAsync(body.WorkspaceId, body.Name, body.Scopes, body.ExpiresAtUtc);
    
    return Results.Ok(new
    {
        apiKeyId = row.Id,
        apiKey = plaintext,
        workspaceId = row.WorkspaceId,
        scopes = row.Scopes.ToString(),
        expiresAtUtc = row.ExpiresAtUtc
    });
})
.WithOpenApi(op =>
{
    op.Summary = "Create an API key.";
    op.Description = "Returns a one-time plaintext API key for the specified workspace and scopes.";
    return op;
});

admin.MapPost("/apikeys/{id:guid}/revoke", async (ApiKeyService keys, Guid id) =>
{
    var ok = await keys.RevokeAsync(id);
    return ok ? Results.Ok(new {revoked = true}) : Results.NotFound();
})
.WithOpenApi(op =>
{
    op.Summary = "Revoke an API key.";
    op.Description = "Marks an API key as revoked for future validation attempts.";
    return op;
});

app.MapAdminEndpoints();


app.MapIngestEndpoints();

app.Run();


record CreateApiKeyRequest(Guid WorkspaceId, string Name, ApiKeyScopes Scopes, DateTimeOffset? ExpiresAtUtc);
record CreateWorkspaceRequest(string Name);
