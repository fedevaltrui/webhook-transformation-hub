using Hub.Domain.Entities;

namespace Hub.Api.Security;

public static class SecurityExtensions
{
    public static RouteGroupBuilder RequireScopes(this RouteGroupBuilder group, ApiKeyScopes scopes)
    {
        group.AddEndpointFilter(new RequireScopesFilter(scopes));
        return group;
    }
}