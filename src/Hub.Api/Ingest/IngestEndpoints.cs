using Hub.Infrastructure;
using Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hub.Api.Ingest;

public static class IngestEndpoints
{
    public static void MapIngestEndpoints(this WebApplication app)
    {
        app.MapPost("/ingest/{endpointKey}", async (
            AppDbContext db,
            HttpRequest req,
            string endpointKey,
            CancellationToken ct) =>
        {
            var ep = await db.Endpoints.FirstOrDefaultAsync(x => x.EndpointKey == endpointKey, ct);
            if (ep is null)
                return Results.NotFound(new { error = "Endpoint not found." });

            if (!ep.IsActive)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var idempotencyKey = req.Headers["Idempotency-Key"].ToString();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await db.IngestRequests
                    .Where(x => x.EndpointId == ep.Id && x.IdempotencyKey == idempotencyKey)
                    .OrderByDescending(x => x.ReceivedAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (existing is not null)
                {
                    // Idempotencia v1: devolvemos el mismo requestId
                    return Results.Accepted(value: new { requestId = existing.Id, duplicated = true });
                }
            }

            var headersJson = RequestCapture.CaptureHeadersJson(req);
            var bodyJson = await RequestCapture.CaptureBodyJsonAsync(req, ct);

            var ingest = new IngestRequest
            {
                EndpointId = ep.Id,
                ReceivedAtUtc = DateTimeOffset.UtcNow,
                Method = req.Method,
                HeadersJson = headersJson,
                BodyJson = bodyJson,
                IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey
            };

            db.IngestRequests.Add(ingest);
            await db.SaveChangesAsync(ct);

            var delivery = new Delivery
            {
                IngestRequestId = ingest.Id,
                Attempt = 1,
                Status = DeliveryStatus.Pending

            };
            db.Deliveries.Add(delivery);
            await db.SaveChangesAsync(ct);

            return Results.Accepted(value: new { requestId = ingest.Id, duplicated = false });
        })
        .WithTags("Ingest")
        .WithOpenApi(op =>
        {
            op.Summary = "Ingest a webhook payload.";
            op.Description = "Captures headers/body, enforces idempotency, and creates a pending delivery.";
            return op;
        });
    }
}
