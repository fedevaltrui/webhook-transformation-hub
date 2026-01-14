using Hub.Domain.Entities;

namespace Hub.Api.Security;


public sealed class RequireScopesFilter : IEndpointFilter
{
    private readonly ApiKeyScopes _required;

    public RequireScopesFilter(ApiKeyScopes required) => _required = required;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var auth = context.HttpContext.RequestServices.GetRequiredService<RequestAuthContext>();

        if(!auth.IsAuthenticated)
        return Results.Unauthorized();

        if((auth.Scopes & _required) != _required)
        return Results.Forbid();

        return await next(context);
    } 
}