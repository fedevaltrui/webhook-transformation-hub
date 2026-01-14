using System.Security.Cryptography.X509Certificates;
using Hub.Domain.Entities;

namespace Hub.Api.Security;

public sealed class RequestAuthContext
{
    public bool IsAuthenticated {get; set;}
    public Guid WorkspaceId {get; set;}
    public Guid ApiKeyId {get; set;}

    public ApiKeyScopes Scopes {get; set;} = ApiKeyScopes.None; 
}