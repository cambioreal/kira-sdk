namespace CambioReal.Kira.Http;

/// <summary>Nomes dos headers próprios da Kira.</summary>
public static class KiraHeaders
{
    /// <summary>Chave de API. Exigida em <em>todo</em> endpoint, inclusive no <c>/auth</c>.</summary>
    public const string ApiKey = "x-api-key";

    /// <summary>Chave de idempotência (UUID v4). TTL de 24 horas no lado da Kira.</summary>
    public const string IdempotencyKey = "idempotency-key";

    /// <summary>
    /// Código OTP de 6 dígitos exigido em payouts fiat, obtido via <c>POST /verification/send</c>.
    /// O nome do header é literalmente <c>x-validation-header</c> — não é um erro de digitação.
    /// </summary>
    public const string ValidationCode = "x-validation-header";

    /// <summary>Assinatura HMAC SHA-256 enviada pela Kira nos webhooks.</summary>
    public const string WebhookSignature = "x-signature-sha256";
}
