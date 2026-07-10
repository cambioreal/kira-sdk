using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>Corpo de <c>POST /v1/payment-link</c>.</summary>
public sealed record CreatePaymentLinkRequest
{
    /// <summary>Usuário que solicita o pagamento.</summary>
    public required string UserId { get; init; }

    /// <summary>Valor solicitado.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public decimal? Amount { get; init; }

    /// <summary>Moeda. Payment links são em USD.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Destino após o pagamento.</summary>
    public Uri? RedirectUri { get; init; }

    /// <summary>Descrição apresentada ao pagador.</summary>
    public string? Description { get; init; }

    /// <summary>Sua referência interna.</summary>
    public string? Reference { get; init; }
}

/// <summary>Payment link criado.</summary>
public sealed record KiraPaymentLink : KiraResponse
{
    /// <summary>Identificador do link.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>URL a apresentar ao pagador.</summary>
    public Uri? Url { get; init; }

    /// <summary>Situação.</summary>
    public string? Status { get; init; }

    /// <summary>Valor.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Amount { get; init; }

    /// <summary>Expiração.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Corpo de <c>POST /webhooks/register</c>. Autentica só com <c>x-api-key</c>.
/// </summary>
public sealed record RegisterWebhookRequest
{
    /// <summary>Endpoint que receberá os eventos. Precisa ser HTTPS.</summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// Segredo usado para assinar os eventos em HMAC SHA-256. Fortemente recomendado.
    /// Guarde-o no <c>pass</c> e verifique com <see cref="Webhooks.KiraWebhookVerifier"/>.
    /// </summary>
    public string? SecretKey { get; init; }
}

/// <summary>Webhook registrado.</summary>
public sealed record KiraWebhookRegistration : KiraResponse
{
    /// <summary>Identificador do registro.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Endpoint registrado.</summary>
    public Uri? Url { get; init; }
}

/// <summary>Subdivisão (estado ou província) de um país.</summary>
public sealed record CountrySubdivision : KiraResponse
{
    /// <summary>Código da subdivisão.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nome da subdivisão.</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>País suportado para endereços de usuário.</summary>
public sealed record KiraCountry : KiraResponse
{
    /// <summary>Código ISO 3166-1.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nome do país.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Estados ou províncias.</summary>
    public IReadOnlyList<CountrySubdivision> Subdivisions { get; init; } = [];
}

/// <summary>Banco disponível para operações de pagamento em um país.</summary>
public sealed record KiraBank : KiraResponse
{
    /// <summary>Código do banco, usado em <see cref="CreatePayInRequest.BankCode"/>.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nome do banco.</summary>
    public string Name { get; init; } = string.Empty;
}
