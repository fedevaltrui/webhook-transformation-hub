namespace Hub.Domain.Entities;

public sealed class ApiKey
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid WorkspaceId {get; set; }
    public string Name {get; set; } = null!;

    //Lookup rapido - evitar scans
    public string KeyPrefix {get; set; }= null!;

    //Hash (PBKDF2) Base64
    public string KeyHash { get; set; } = null!;

    //Salt Base64 x key

    public string KeySalt { get; set;} = null!;

    public int KeyIterations { get; set; }
    
    public ApiKeyScopes Scopes {get; set; } = ApiKeyScopes.None;


    public DateTimeOffset CreatedAtUtc {get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc{get; set; }
    public DateTimeOffset? RevokedAtUtc{get; set; }
    public DateTimeOffset? LastUsedAtUtc {get; set;}

    public Workspace Workspace {get; set;} = null!;
}