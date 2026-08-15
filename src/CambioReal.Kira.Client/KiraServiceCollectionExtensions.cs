using CambioReal.Kira.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CambioReal.Kira;

/// <summary>Registro do cliente Kira no container.</summary>
public static class KiraServiceCollectionExtensions
{
    /// <summary>
    /// Registra o cliente a partir de uma seção de configuração.
    /// </summary>
    /// <remarks>
    /// As credenciais precisam chegar por um provider seguro (variáveis de ambiente, user-secrets,
    /// Vault). Nunca versione <c>ClientId</c>, <c>Password</c> ou <c>ApiKey</c> em
    /// <c>appsettings.json</c> — a fonte da verdade é o <c>pass</c>, grupo <c>kira/</c>.
    /// </remarks>
    /// <param name="services">Container.</param>
    /// <param name="configuration">Seção com as chaves de <see cref="KiraOptions"/>.</param>
    public static IServiceCollection AddKiraClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return services.AddKiraClient(configuration.Bind);
    }

    /// <summary>
    /// Registra <see cref="KiraClient"/>, o provedor de token e o pipeline HTTP autenticado.
    /// </summary>
    /// <param name="services">Container.</param>
    /// <param name="configure">Configuração das opções.</param>
    public static IServiceCollection AddKiraClient(this IServiceCollection services, Action<KiraOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddOptions<KiraOptions>().Validate(
            options =>
            {
                try
                {
                    options.Validate();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            },
            "A configuração do KiraOptions é inválida.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IKiraTokenProvider, KiraTokenProvider>();
        services.TryAddTransient<KiraAuthenticationHandler>();

        // Cliente exclusivo do POST /auth: sem o handler de autenticação, para não recorrer.
        services.AddHttpClient(KiraClientNames.Auth, ConfigureTransport);

        services.AddHttpClient<KiraClient>(KiraClientNames.Api, ConfigureTransport)
            .AddHttpMessageHandler<KiraAuthenticationHandler>();

        return services;
    }

    private static void ConfigureTransport(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KiraOptions>>().Value;
        options.Validate();

        client.BaseAddress = options.ResolveBaseAddress();
        client.Timeout = options.Timeout;
    }
}
