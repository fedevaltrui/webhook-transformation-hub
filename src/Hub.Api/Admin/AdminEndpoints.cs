using System.Text.Json;
using Hub.Api.Security;
using Hub.Domain.Entities;
using Hub.Infrastructure;
using Hub.Api.Ingest;
using Microsoft.EntityFrameworkCore;
using DbEndpoint = Hub.Domain.Entities.Endpoint;

namespace Hub.Api.Admin;




public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        //Group ADMIN
        var admin = app.MapGroup("/admin").RequireScopes(ApiKeyScopes.Admin).WithTags("Admin");

        admin.MapPost("/endpoints", async (
            AppDbContext db,
            RequestAuthContext auth,
            CreateEndpointRequest body) =>
        {
            //Workspace multi-tenant
            if(!auth.IsAuthenticated)
                return Results.Unauthorized();  

            if((auth.Scopes & ApiKeyScopes.Admin) != ApiKeyScopes.Admin )
                return Results.Forbid();

            if(string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { error = "Name is required."});

            if(!Uri.TryCreate(body.DestinationUrl, UriKind.Absolute, out var uri))
                return Results.BadRequest(new{error = "DestinationUrl must be a valid absolute URL."});

              // Generar endpointKey y minimizar colisiones (retry)
            string endpointKey;
            var attempts = 0;
            while (true)
            {
                attempts++;
                endpointKey = RequestCapture.GenerateEndpointKey();

                var exists = await db.Endpoints.AnyAsync(x => x.EndpointKey == endpointKey);
                if (!exists) break;

                if (attempts >= 5)
                    return Results.Problem("Failed to generate unique endpointKey.");
            }

            var ep = new DbEndpoint
            {
                WorkspaceId = auth.WorkspaceId,
                Name = body.Name.Trim(),
                EndpointKey = endpointKey,
                DestinationUrl = uri.ToString(),
                SigningSecret = string.IsNullOrWhiteSpace(body.SigningSecret) ? null : body.SigningSecret,
                IsActive = body.IsActive
            };

            db.Endpoints.Add(ep);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                id = ep.Id,
                ep.Name,
                ep.EndpointKey,
                ep.DestinationUrl,
                ep.IsActive,
                ep.CreatedAtUtc
            });
        })
        .WithSummary("Create an endpoint.")
        .WithDescription("Registers a destination URL and returns the public endpointKey.");

        static JsonElement TryParseJson(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement.Clone();
            }
            catch
            {
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { raw }));
                return doc.RootElement.Clone();
            }
        }
 admin.MapGet("/endpoints", async (AppDbContext db, RequestAuthContext auth) =>
        {
            if (!auth.IsAuthenticated)
                return Results.Unauthorized();

            if ((auth.Scopes & ApiKeyScopes.Admin) != ApiKeyScopes.Admin)
                return Results.Forbid();

            var items = await db.Endpoints
                .Where(x => x.WorkspaceId == auth.WorkspaceId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.EndpointKey,
                    x.DestinationUrl,
                    x.IsActive,
                    x.CreatedAtUtc
                })
                .ToListAsync();

            return Results.Ok(items);
        })
        .WithSummary("List endpoints.")
        .WithDescription("Returns all endpoints for the authenticated workspace.");
        admin.MapGet("/endpoints/{endpointId:guid}/ingests", async (
    AppDbContext db,
    RequestAuthContext auth,
    Guid endpointId,
    int? limit,
    CancellationToken ct) =>
{
    var take = Math.Clamp(limit ?? 50, 1, 200);

    // Aislamiento multi-tenant: el endpoint debe pertenecer al workspace autenticado
    var endpointOwned = await db.Endpoints
        .AnyAsync(e => e.Id == endpointId && e.WorkspaceId == auth.WorkspaceId, ct);

    if (!endpointOwned)
        return Results.NotFound(new { error = "Endpoint not found for this workspace." });

    var items = await db.IngestRequests
        .Where(x => x.EndpointId == endpointId)
        .OrderByDescending(x => x.ReceivedAtUtc)
        .Take(take)
        .Select(x => new
        {
            x.Id,
            x.ReceivedAtUtc,
            x.Method,
            x.IdempotencyKey,
            lastDelivery = x.Deliveries
                .OrderByDescending(d => d.Attempt)
                .Select(d => new
                {
                    d.Status,
                    d.Attempt,
                    d.ResponseStatusCode,
                    d.Error,
                    d.StartedAtUtc,
                    d.FinishedAtUtc,
                    d.NextAttemptAtUtc
                })
                .FirstOrDefault()
        })
        .ToListAsync(ct);

    return Results.Ok(new { endpointId, count = items.Count, items });
})
.WithSummary("List ingests for an endpoint.")
.WithDescription("Returns recent ingests with the latest delivery status.");

admin.MapGet("/ingests/{ingestId:guid}", async (
    AppDbContext db,
    RequestAuthContext auth,
    Guid ingestId,
    CancellationToken ct) =>
{
    // Join explícito para asegurar workspace isolation
    var result = await (from i in db.IngestRequests
                        join e in db.Endpoints on i.EndpointId equals e.Id
                        where i.Id == ingestId && e.WorkspaceId == auth.WorkspaceId
                        select new
                        {
                            i.Id,
                            i.EndpointId,
                            endpointName = e.Name,
                            i.ReceivedAtUtc,
                            i.Method,
                            i.IdempotencyKey,
                            headers = i.HeadersJson,
                            body = i.BodyJson,
                            deliveries = db.Deliveries
                                .Where(d => d.IngestRequestId == i.Id)
                                .OrderByDescending(d => d.Attempt)
                                .Select(d => new
                                {
                                    d.Id,
                                    d.Attempt,
                                    d.Status,
                                    d.ResponseStatusCode,
                                    d.Error,
                                    d.StartedAtUtc,
                                    d.FinishedAtUtc,
                                    d.NextAttemptAtUtc
                                })
                                .ToList()
                        }).FirstOrDefaultAsync(ct);

    if (result is null)
        return Results.NotFound(new { error = "Ingest not found for this workspace." });

    // Parseo para devolver JSON legible (headers/body son jsonb strings)
    var headersEl = TryParseJson(result.headers);
    var bodyEl = TryParseJson(result.body);

    return Results.Ok(new
    {
        result.Id,
        result.EndpointId,
        result.endpointName,
        result.ReceivedAtUtc,
        result.Method,
        result.IdempotencyKey,
        headers = headersEl,
        body = bodyEl,
        result.deliveries
    });
})
.WithSummary("Get a single ingest.")
.WithDescription("Returns captured headers/body and delivery attempts for a specific ingest.");


        }
        public sealed record CreateEndpointRequest(
        string Name,
        string DestinationUrl,
        bool IsActive,
        string? SigningSecret
    );
}
