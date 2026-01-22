namespace Hub.Infrastructure.Security;

public sealed class DeliveryOptions
{
    public int PollSeconds { get; set; } = 2;
    public int MaxAttempts { get; set; } = 5;
    public int BaseDelaySeconds { get; set; } = 3;
    public int MaxDelaySeconds { get; set; } = 60;
    public int HttpTimeoutSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 10;
}
