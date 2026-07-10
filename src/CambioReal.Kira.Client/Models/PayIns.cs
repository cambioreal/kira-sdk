using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/payins</c>.
/// </summary>
/// <remarks>
/// O comportamento muda por método:
/// <list type="bullet">
///   <item><description><see cref="PayInMethod.Pse"/> (Colômbia, COP): <b>uso único</b>. Exige
///   <see cref="Amount"/> e <see cref="BankCode"/>.</description></item>
///   <item><description><see cref="PayInMethod.Spei"/> (México, MXN): <b>reutilizável</b>. Não aceita
///   <see cref="Amount"/> nem <see cref="BankCode"/> — o pagador escolhe o valor.</description></item>
/// </list>
/// Não há PIX: a Kira não coleta pagamentos originados no Brasil.
/// Os dados do pagador vêm do cadastro do usuário, via <see cref="UserId"/>.
/// </remarks>
public sealed record CreatePayInRequest
{
    /// <summary>Usuário cujos dados identificam o pagador.</summary>
    public required string UserId { get; init; }

    /// <summary>Método de coleta.</summary>
    public required PayInMethod Method { get; init; }

    /// <summary>Valor. Exigido em PSE, ausente em SPEI.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public decimal? Amount { get; init; }

    /// <summary>Banco do pagador. Exigido em PSE. Obtenha em <c>GET /banks</c>.</summary>
    public string? BankCode { get; init; }

    /// <summary>Sua referência interna.</summary>
    public string? Reference { get; init; }
}

/// <summary>Liquidação de um PayIn na blockchain.</summary>
public sealed record PayInSettlement : KiraResponse
{
    /// <summary>Valor liquidado.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; init; }

    /// <summary>Moeda liquidada.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Hash da transação.</summary>
    public string? TransactionHash { get; init; }

    /// <summary>Situação.</summary>
    public string? Status { get; init; }

    /// <summary>Momento da liquidação.</summary>
    public DateTimeOffset? SettledAt { get; init; }
}

/// <summary>PayIn devolvido pela Kira.</summary>
public sealed record KiraPayIn : KiraResponse
{
    /// <summary>Identificador do PayIn.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Usuário pagador.</summary>
    public string? UserId { get; init; }

    /// <summary>Método de coleta.</summary>
    public PayInMethod? Method { get; init; }

    /// <summary>Link de pagamento a apresentar ao pagador.</summary>
    public Uri? PaymentLink { get; init; }

    /// <summary>Situação.</summary>
    public string? Status { get; init; }

    /// <summary>Valor, quando fixo.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Amount { get; init; }

    /// <summary>
    /// Liquidações. PSE devolve um elemento (uso único); SPEI devolve vários, um por pagamento.
    /// </summary>
    public IReadOnlyList<PayInSettlement> Settlement { get; init; } = [];

    /// <summary>Criação.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Corpo de <c>POST /v1/payins/fees</c>. A cotação vale 10 minutos.</summary>
public sealed record PayInFeesRequest
{
    /// <summary>Método de coleta.</summary>
    public required PayInMethod Method { get; init; }

    /// <summary>Valor que o pagador enviará.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public required decimal Amount { get; init; }

    /// <summary>Moeda de origem.</summary>
    public Currency? Currency { get; init; }
}

/// <summary>
/// Resposta de <c>POST /v1/payins/fees</c>.
/// </summary>
/// <remarks>
/// A ordem das deduções importa: o valor do pagador perde a taxa de coleta, depois a de liquidação,
/// e só o restante é convertido pela <see cref="KiraRate"/>.
/// </remarks>
public sealed record PayInFeesResponse : KiraResponse
{
    /// <summary>Taxa de coleta (PSE ou SPEI).</summary>
    public FeeComponent? CollectionFees { get; init; }

    /// <summary>Taxa de liquidação na blockchain.</summary>
    public FeeComponent? SettlementFees { get; init; }

    /// <summary>Câmbio de mercado.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Rate { get; init; }

    /// <summary>Câmbio efetivo aplicado pela Kira, já com markup.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? KiraRate { get; init; }

    /// <summary>Markup sobre o câmbio, em pontos percentuais. Tipicamente 5%.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? FxMarkup { get; init; }

    /// <summary>Valor que você receberá, líquido.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? FinalAmount { get; init; }

    /// <summary>Expiração da cotação.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
