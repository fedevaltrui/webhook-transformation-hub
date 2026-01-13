using System.Net;

namespace Hub.Domain.Entities;

public sealed class Workspace
{
    public Guid Id {get;set;} = Guid.NewGuid();
    public string Name {get;set;} = null!;
    public DateTimeOffset CreatedAtUtc {get; set; } = DateTimeOffset.UtcNow;

    public List<ApiKey> ApiKeys {get; set;} = new();
    public List<Endpoint> Endpoints {get; set;} = new();

}