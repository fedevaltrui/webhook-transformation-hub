namespace Hub.Domain.Entities;

public sealed class Endpoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId {get; set; }

    public string Name { get; set;} = null!;
    public string EndpointKey { get; set; } = null!; // clave publica para /ingest/{endopintKey} 
    public string DestinationUrl {get; set;} = null!;
    public string? SigningSecret {get; set;}  //para HMAC en issue posterior
    public bool IsActive {get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Workspace Workspace {get; set;} = null!;
    public List<IngestRequest> IngestRequests {get; set;} = new();

    

}