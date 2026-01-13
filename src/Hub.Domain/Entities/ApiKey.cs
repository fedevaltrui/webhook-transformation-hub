namespace Hub.Domain.Entities;

public sealed class ApiKey
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid WorkspaceId {get; set; }
    public string Name {get; set; } = null!;
    public string KeyHash { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc {get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAtUtc {get; set;}

    public Workspace Workspace {get; set;} = null!;
}