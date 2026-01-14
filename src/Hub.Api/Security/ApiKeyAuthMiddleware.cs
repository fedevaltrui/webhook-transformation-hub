using Hub.Domain.Entities;
using Hub.Infrastructure.Security;
using Serilog.Context;

namespace Hub.Api.Security;

public sealed class ApiKeyAuthMiddleware : IMiddleware
{
    private const string Header = "X-Api-Key";

    private readonly ApiKeyService _svc;
    private readonly RequestAuthContext _ctx;

    public ApiKeyAuthMiddleware(ApiKeyService svc, RequestAuthContext ctx)
    {
        _svc = svc;
        _ctx = ctx;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var key = context.Request.Headers[Header].ToString();

        if (!string.IsNullOrWhiteSpace(key))
        {
            var res = await _svc.ValidateAsync(key);
            if (res is not null)
            {
                var apiKey = res.Value.ApiKey;

                _ctx.IsAuthenticated = true;
                _ctx.ApiKeyId = apiKey.Id;
                _ctx.WorkspaceId = apiKey.WorkspaceId;
                _ctx.Scopes = apiKey.Scopes;

                //Enriquecemos LogContext con Serilog
                using (LogContext.PushProperty("WorkspaceId",apiKey.WorkspaceId))
                using (LogContext.PushProperty("ApiKeyId", apiKey.Id))
                using (LogContext.PushProperty("ApiKeyScopes", apiKey.Scopes.ToString()))
                {
                    await next(context);
                    return;
                }
            }
        }
        await next(context);
    }
}