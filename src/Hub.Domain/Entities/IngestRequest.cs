namespace Hub.Domain.Entities;

public sealed class IngestRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EndpointId { get; set; }

    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Method{get; set; } = "POST";

    public string HeadersJson {get; set; } = "{}";
    public string BodyJson { get; set; } = "{}";

    public string? IdempotencyKey {get; set; }

    public Endpoint Endpoint {get; set; } = null!;
    public List<Delivery> Deliveries{get; set;} = new();

}