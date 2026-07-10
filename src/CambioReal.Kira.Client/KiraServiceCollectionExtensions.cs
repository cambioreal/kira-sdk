using CambioReal.Kira.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CambioReal.Kira;

/// <summary>Registro do cliente Kira no container.</summary>
public static class KiraServiceCollectionExtensions
{
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
                options.Validate();
                return true;
            },
            "A configuração do KiraOptions é inválida.");

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
