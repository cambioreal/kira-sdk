using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/payins</c>.
/// </summary>
/// <remarks>
/// <b>Reescrito em 2026-07-14</b> a partir de erros de validação reais do sandbox, confirmado até
/// a camada de negócio (chegou a <c>INVALID_BANK_CODE</c>, não mais a erro de schema) para
/// <see cref="PayInMethod.Pse"/>: o campo discriminador real é <c>type</c>, não <c>method</c>; e
/// faltavam três campos obrigatórios inteiros — <see cref="Currency"/>, <see cref="CallbackUrl"/>
/// e <see cref="Settlement"/>.
/// <para>
/// <b>Achado importante:</b> ao testar <c>type=SPEI</c> contra o sandbox, a validação rejeitou
/// com <c>Expected 'PSE', received 'SPEI'</c> — ou seja, <c>SPEI</c> não foi aceito como valor
/// válido neste cliente/ambiente, apesar de documentado. Pode ser uma limitação do sandbox, do
/// client de teste, ou a doc estar errada sobre o rail existir nesta forma. Tratar como não
/// confirmado até verificar novamente.
/// </para>
/// Não há PIX: a Kira não coleta pagamentos originados no Brasil. Os dados do pagador vêm do
/// cadastro do usuário, via <see cref="UserId"/>.
/// </remarks>
public sealed record CreatePayInRequest
{
    /// <summary>Usuário cujos dados identificam o pagador.</summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Método de coleta. Campo JSON real é <c>type</c>, não <c>method</c> — confirmado contra o
    /// sandbox em 2026-07-14 (a validação não reconhecia <c>method</c> como campo esperado).
    /// </summary>
    public required PayInMethod Type { get; init; }

    /// <summary>Valor. Confirmado obrigatório para PSE.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public decimal? Amount { get; init; }

    /// <summary>
    /// Moeda de origem. Confirmado obrigatório contra o sandbox em 2026-07-14 — campo inteiro
    /// ausente do modelo anterior.
    /// </summary>
    public required Currency Currency { get; init; }

    /// <summary>Banco do pagador. Confirmado obrigatório para PSE. Obtenha em <c>GET /banks</c>.</summary>
    public string? BankCode { get; init; }

    /// <summary>
    /// URL que recebe notificações deste PayIn. Confirmado obrigatório contra o sandbox em
    /// 2026-07-14 — campo inteiro ausente do modelo anterior.
    /// </summary>
    public required Uri CallbackUrl { get; init; }

    /// <summary>
    /// Para onde os fundos liquidados são enviados. Confirmado obrigatório contra o sandbox em
    /// 2026-07-14 — campo inteiro ausente do modelo anterior. Reutiliza o mesmo formato de conta
    /// de <see cref="CreateRecipientRequest.Account"/> (mesmo discriminador <c>account_type</c>).
    /// </summary>
    public required PayInSettlementInput Settlement { get; init; }

    /// <summary>Sua referência interna.</summary>
    public string? Reference { get; init; }
}

/// <summary>
/// Beneficiário da liquidação de um <see cref="CreatePayInRequest"/>.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14: mesma exigência de <c>CreateRecipientRequest</c> —
/// <c>first_name</c>/<c>last_name</c> (pessoa física) ou <c>company_name</c> (pessoa jurídica) é
/// obrigatório junto de <see cref="Account"/>.
/// </remarks>
public sealed record PayInSettlementInput
{
    /// <summary>Nome do beneficiário, para pessoa física.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome do beneficiário, para pessoa física.</summary>
    public string? LastName { get; init; }

    /// <summary>Razão social do beneficiário, para pessoa jurídica.</summary>
    public string? CompanyName { get; init; }

    /// <summary>
    /// Conta de destino. Testado com sucesso (passou da validação de schema) usando
    /// <see cref="Models.AccountType.Wallet"/>.
    /// </summary>
    public required RecipientAccount Account { get; init; }
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
