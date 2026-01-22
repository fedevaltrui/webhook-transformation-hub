using System.Net.Http.Headers;
using System.Text;
using Hub.Infrastructure;
using Hub.Infrastructure.Security;
using Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public sealed class DeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<DeliveryOptions> _options;
    private readonly ILogger<DeliveryWorker> _logger;

    public DeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<DeliveryOptions> options,
        ILogger<DeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var opt = _options.CurrentValue;
                var processed = await ProcessBatchAsync(opt, stoppingToken);

                // Si no hubo trabajo, dormimos; si hubo, iteramos rápido
                var delay = processed == 0
                    ? TimeSpan.FromSeconds(opt.PollSeconds)
                    : TimeSpan.FromMilliseconds(200);

                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeliveryWorker loop error");
                await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(DeliveryOptions opt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var client = _httpClientFactory.CreateClient("Delivery");

        var now = DateTimeOffset.UtcNow;

        // 1) Claim atómico con FOR UPDATE SKIP LOCKED para evitar doble procesamiento
        List<Delivery> pending;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            pending = await db.Deliveries
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Deliveries"
                    WHERE "Status" = {(int)DeliveryStatus.Pending}
                      AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= {now})
                    ORDER BY "NextAttemptAtUtc" NULLS FIRST
                    LIMIT {opt.BatchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct);

            if (pending.Count == 0)
            {
                await tx.CommitAsync(ct);
                return 0;
            }

            foreach (var delivery in pending)
            {
                delivery.Status = DeliveryStatus.InProgress;
                delivery.StartedAtUtc = now;
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        // 2) Procesar uno a uno
        var count = 0;
        foreach (var delivery in pending)
        {
            ct.ThrowIfCancellationRequested();

            await db.Entry(delivery)
                .Reference(x => x.IngestRequest)
                .Query()
                .Include(ir => ir.Endpoint)
                .LoadAsync(ct);

            var endpoint = delivery.IngestRequest.Endpoint;
            var ingest = delivery.IngestRequest;

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, endpoint.DestinationUrl);

                // Payload: BodyJson guardado (se asume JSON válido)
                req.Content = new StringContent(ingest.BodyJson ?? "{}", Encoding.UTF8, "application/json");

                // Metadata headers
                req.Headers.Add("X-Hub-Ingest-Id", ingest.Id.ToString());
                req.Headers.Add("X-Hub-Delivery-Id", delivery.Id.ToString());
                req.Headers.Add("X-Hub-Attempt", delivery.Attempt.ToString());

                var resp = await client.SendAsync(req, ct);

                delivery.ResponseStatusCode = (int)resp.StatusCode;
                delivery.FinishedAtUtc = DateTimeOffset.UtcNow;

                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode <= 299)
                {
                    delivery.Status = DeliveryStatus.Success;
                    delivery.Error = null;
                }
                else
                {
                    await ScheduleRetryOrFailAsync(db, delivery, $"Non-2xx: {(int)resp.StatusCode}", opt, ct);
                }

                await db.SaveChangesAsync(ct);
                count++;
            }
            catch (Exception ex)
            {
                await ScheduleRetryOrFailAsync(db, delivery, ex.GetType().Name + ": " + ex.Message, opt, ct);
                delivery.FinishedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogWarning(ex, "Delivery failed deliveryId={DeliveryId} attempt={Attempt}", delivery.Id, delivery.Attempt);
                count++;
            }
        }

        return count;
    }

    private static Task ScheduleRetryOrFailAsync(AppDbContext db, Delivery delivery, string error, DeliveryOptions opt, CancellationToken ct)
    {
        delivery.Error = error;

        if (delivery.Attempt >= opt.MaxAttempts)
        {
            delivery.Status = DeliveryStatus.Failed;
            delivery.NextAttemptAtUtc = null;
            return Task.CompletedTask;
        }

        delivery.Attempt += 1;
        delivery.Status = DeliveryStatus.Pending;

        var backoff = ComputeBackoff(delivery.Attempt, opt);
        delivery.NextAttemptAtUtc = DateTimeOffset.UtcNow.Add(backoff);

        return Task.CompletedTask;
    }

    private static TimeSpan ComputeBackoff(int attempt, DeliveryOptions opt)
    {
        // exponencial simple con cap
        var seconds = opt.BaseDelaySeconds * Math.Pow(2, Math.Max(0, attempt - 2));
        seconds = Math.Min(seconds, opt.MaxDelaySeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
