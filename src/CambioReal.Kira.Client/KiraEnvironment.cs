namespace CambioReal.Kira;

/// <summary>Ambiente da API Kira.</summary>
public enum KiraEnvironment
{
    /// <summary>Sandbox. Verifica usuários automaticamente e só oferece o provedor <c>slovak_savings_bank</c>.</summary>
    Sandbox = 0,

    /// <summary>Produção.</summary>
    Production = 1,
}

/// <summary>Resolve o endereço base de cada <see cref="KiraEnvironment"/>.</summary>
public static class KiraEnvironmentExtensions
{
    /// <summary>
    /// Endereço base do ambiente.
    /// </summary>
    /// <remarks>
    /// O sandbox da Kira é um <em>prefixo de path</em> (<c>/sandbox</c>), não um subdomínio. Isso torna a
    /// barra final obrigatória: <see cref="Uri"/> resolve um path relativo contra o último segmento do base
    /// address, então <c>https://api.balampay.com/sandbox</c> + <c>v1/users</c> produziria
    /// <c>https://api.balampay.com/v1/users</c> — perdendo o <c>/sandbox</c> silenciosamente e apontando
    /// requisições de teste para produção. Com a barra final, o resultado é
    /// <c>https://api.balampay.com/sandbox/v1/users</c>.
    /// Pelo mesmo motivo, todo path passado ao cliente é relativo e não pode começar com <c>/</c>.
    /// </remarks>
    public static Uri GetBaseAddress(this KiraEnvironment environment) => environment switch
    {
        KiraEnvironment.Production => new Uri("https://api.balampay.com/", UriKind.Absolute),
        KiraEnvironment.Sandbox => new Uri("https://api.balampay.com/sandbox/", UriKind.Absolute),
        _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, "Ambiente Kira desconhecido."),
    };
}
