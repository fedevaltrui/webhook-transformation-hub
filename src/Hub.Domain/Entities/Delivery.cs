namespace Hub.Domain.Entities;

public enum DeliveryStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    InProgress = 3
}

public sealed class Delivery
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid IngestRequestId {get; set;}

    public int Attempt {get; set;} = 1;
    public DeliveryStatus Status{get; set;} = DeliveryStatus.Pending;
    public DateTimeOffset CreatedAtUtc {get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc {get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }

    public int? ResponseStatusCode {get; set; }
    public string? Error {get; set;}

    public IngestRequest IngestRequest{ get; set; } = null!;
}
