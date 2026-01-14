using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Hub.Infrastructure.Security;


namespace Hub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Postgres")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:Postgres");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(cs));

        services.AddSingleton(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var opt = cfg.GetSection("Security").Get<SecurityOptions>() ?? new SecurityOptions();

        if (string.IsNullOrWhiteSpace(opt.ApiKeyPepper))
            throw new InvalidOperationException("Missing Security:ApiKeyPepper");

        if (string.IsNullOrWhiteSpace(opt.BootstrapToken))
            opt.BootstrapToken = "DEV_ONLY";

        return opt;
    });

services.AddSingleton<ApiKeyCrypto>();

services.AddScoped<ApiKeyService>();

        return services;
    }
}