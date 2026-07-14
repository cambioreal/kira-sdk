namespace CambioReal.Kira.Http;

/// <summary>Modificadores por requisição.</summary>
public sealed record KiraRequestContext
{
    /// <summary>Contexto vazio: bearer + <c>x-api-key</c>, sem idempotência e sem OTP.</summary>
    public static KiraRequestContext Default { get; } = new();

    /// <summary>
    /// Chave de idempotência. Obrigatória nos POSTs de criação (usuário, verificação, conta virtual,
    /// carteira, recipient, payout). Reenviar a mesma chave com corpo idêntico devolve a resposta
    /// armazenada (200 em vez de 201); com corpo diferente, a Kira responde 409.
    /// </summary>
    public Guid? IdempotencyKey { get; init; }

    /// <summary>Código OTP de 6 dígitos, exigido em payouts fiat.</summary>
    public string? ValidationCode { get; init; }

    /// <summary>
    /// Envia apenas <c>x-api-key</c>, sem <c>Authorization: Bearer</c>.
    /// </summary>
    /// <remarks>
    /// A doc descrevia <c>POST /webhooks/register</c> como o único endpoint que dispensa o JWT —
    /// confirmado <b>errado</b> contra o sandbox em 2026-07-14 (esse endpoint exige Bearer;
    /// ver <see cref="Resources.PlatformResource.RegisterWebhookAsync"/>). Nenhum endpoint
    /// confirmado usa esta opção hoje; mantida como escape hatch para um caso ainda não
    /// descoberto.
    /// </remarks>
    public bool SkipBearerAuthentication { get; init; }

    /// <summary>Contexto com uma chave de idempotência recém-gerada.</summary>
    public static KiraRequestContext WithNewIdempotencyKey() => new() { IdempotencyKey = Guid.NewGuid() };
}

/// <summary>Chaves de <see cref="HttpRequestOptions"/> usadas para comunicar o contexto ao pipeline.</summary>
internal static class KiraRequestOptionKeys
{
    public static readonly HttpRequestOptionsKey<bool> SkipBearerAuthentication = new("kira.skip_bearer");
}
