using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>Como o payout será financiado com stablecoin. A presença deste objeto define o modo cripto.</summary>
public sealed record PaymentInstructions
{
    /// <summary>Blockchain de onde virão os fundos.</summary>
    public required WalletNetwork Network { get; init; }

    /// <summary>Stablecoin usada para financiar.</summary>
    public required WalletToken Token { get; init; }
}

/// <summary>Documento comprobatório de um payout cripto. Obrigatório nesse modo.</summary>
public sealed record SupportingDocument
{
    /// <summary>Natureza do documento.</summary>
    public required SupportingDocumentType Type { get; init; }

    /// <summary>Arquivo como data URI base64 (<c>data:application/pdf;base64,…</c>). PDF, PNG ou JPG.</summary>
    public required string File { get; init; }

    /// <summary>Descrição, até 255 caracteres.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// Corpo de <c>POST /v1/virtual-accounts/{id}/payout/preview</c>. Não cria nada.
/// </summary>
/// <remarks>
/// Informe <see cref="RecipientId"/> <em>ou</em> <see cref="AccountType"/>. Com
/// <see cref="AccountType.Wallet"/> e sem recipient, <see cref="WalletNetwork"/> e
/// <see cref="WalletToken"/> passam a ser obrigatórios.
/// </remarks>
public sealed record PreviewPayoutRequest
{
    /// <summary>Recipient salvo.</summary>
    public string? RecipientId { get; init; }

    /// <summary>Rail, para estimar sem um recipient salvo.</summary>
    public AccountType? AccountType { get; init; }

    /// <summary>Valor a enviar.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public required decimal Amount { get; init; }

    /// <summary>Blockchain, quando o rail é carteira e não há recipient salvo.</summary>
    public WalletNetwork? WalletNetwork { get; init; }

    /// <summary>Stablecoin, quando o rail é carteira e não há recipient salvo.</summary>
    public WalletToken? WalletToken { get; init; }

    /// <summary>Presente ⇒ payout cripto (o chamador financia com stablecoin).</summary>
    public PaymentInstructions? PaymentInstructions { get; init; }

    /// <summary>
    /// Reserva uma quote junto com a prévia, devolvendo <see cref="PreviewPayoutResponse.QuoteId"/>.
    /// A quote trava valor, taxas e câmbio; resgate-a em <see cref="InitiatePayoutRequest.QuoteId"/>.
    /// </summary>
    public bool? CreateQuote { get; init; }
}

/// <summary>Componente fixo e percentual de uma taxa.</summary>
public sealed record FeeComponent : KiraResponse
{
    /// <summary>Parcela fixa.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal FixedFee { get; init; }

    /// <summary>Parcela percentual.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal PercentageFee { get; init; }
}

/// <summary>Decomposição das taxas de um payout.</summary>
public sealed record PayoutFees : KiraResponse
{
    /// <summary>Taxa base da Kira.</summary>
    public FeeComponent? BaseFees { get; init; }

    /// <summary>Markup configurado para o seu cliente.</summary>
    public FeeComponent? ClientMarkup { get; init; }

    /// <summary>Soma das taxas.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal TotalFees { get; init; }
}

/// <summary>Resposta de <c>POST /v1/virtual-accounts/{id}/payout/preview</c>.</summary>
public sealed record PreviewPayoutResponse : KiraResponse
{
    /// <summary>Valor bruto.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; init; }

    /// <summary>Taxas.</summary>
    public PayoutFees? Fees { get; init; }

    /// <summary>Valor líquido que o beneficiário recebe.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal RecipientAmount { get; init; }

    /// <summary>Moeda recebida pelo beneficiário. Em payout fiat→cripto, é USDC ou USDT.</summary>
    public Currency? RecipientCurrency { get; init; }

    /// <summary>Quote reservada, quando <see cref="PreviewPayoutRequest.CreateQuote"/> foi verdadeiro.</summary>
    public string? QuoteId { get; init; }

    /// <summary>
    /// Expiração da quote.
    /// </summary>
    /// <remarks>
    /// <b>Contradição conhecida.</b> O guia de Quotations afirma 10 minutos; a API Reference do
    /// <c>initiatePayout</c> afirma 15. Prefira este campo ao invés de assumir qualquer um dos dois,
    /// e trate a expiração como caminho normal, não excepcional.
    /// </remarks>
    public DateTimeOffset? QuoteExpiresAt { get; init; }
}

/// <summary>
/// Corpo de <c>POST /v1/virtual-accounts/{id}/payout</c>. Exige chave de idempotência.
/// </summary>
/// <remarks>
/// Dois modos, distinguidos apenas pela presença de <see cref="PaymentInstructions"/>:
/// <list type="bullet">
///   <item><description><b>Fiat</b> (sem <c>payment_instructions</c>): debita o saldo da conta virtual.
///   Segundo a API Reference, exige OTP no header <c>x-validation-header</c> — obtenha-o em
///   <c>POST /verification/send</c>. O guia de Payouts diz que o OTP é opcional e, três parágrafos
///   depois, lista o erro <c>VALIDATION_REQUIRED</c> para o header ausente. Assuma obrigatório.</description></item>
///   <item><description><b>Cripto</b> (com <c>payment_instructions</c>): a Kira devolve um endereço de
///   depósito de uso único. Exige <see cref="SupportingDocuments"/>. Não exige OTP.</description></item>
/// </list>
/// A conta virtual precisa ser <c>US_BANK</c> e estar ativa. Há validação cruzada: conta fiat rejeita
/// <c>payment_instructions</c>, conta cripto os exige.
/// </remarks>
public sealed record InitiatePayoutRequest
{
    /// <summary>Recipient de destino.</summary>
    public required string RecipientId { get; init; }

    /// <summary>Valor a enviar.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public required decimal Amount { get; init; }

    /// <summary>
    /// Quote reservada previamente. Trava valor, taxas e câmbio. Omitir precifica na hora da execução.
    /// </summary>
    public string? QuoteId { get; init; }

    /// <summary>Presente ⇒ payout cripto.</summary>
    public PaymentInstructions? PaymentInstructions { get; init; }

    /// <summary>Comprovantes. Obrigatórios no modo cripto.</summary>
    public IReadOnlyList<SupportingDocument>? SupportingDocuments { get; init; }

    /// <summary>Sua referência interna.</summary>
    public string? Reference { get; init; }

    /// <summary>Descrição livre.</summary>
    public string? Description { get; init; }
}

/// <summary>Endereço de uso único para financiar um payout cripto.</summary>
public sealed record PayoutDepositInstructions : KiraResponse
{
    /// <summary>Endereço da carteira que deve receber a stablecoin.</summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>Blockchain.</summary>
    public WalletNetwork? Network { get; init; }

    /// <summary>Stablecoin esperada.</summary>
    public WalletToken? Token { get; init; }

    /// <summary>Depois disto o endereço não recebe mais.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Resposta de <c>POST /v1/virtual-accounts/{id}/payout</c>.
/// </summary>
/// <remarks>
/// Não existe endpoint documentado para consultar um payout depois de criado. O único jeito de
/// acompanhar o status é o webhook <c>transaction_update</c> — o que torna webhooks um componente
/// obrigatório da integração, não opcional.
/// </remarks>
public sealed record InitiatePayoutResponse : KiraResponse
{
    /// <summary>Identificador do payout.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Situação inicial.</summary>
    public PayoutStatus? Status { get; init; }

    /// <summary>Valor.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; init; }

    /// <summary>Taxas aplicadas.</summary>
    public PayoutFees? Fees { get; init; }

    /// <summary>Endereço de depósito, presente apenas no modo cripto.</summary>
    public PayoutDepositInstructions? DepositInstructions { get; init; }

    /// <summary>Sua referência interna, ecoada.</summary>
    public string? Reference { get; init; }

    /// <summary>Criação.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Corpo de <c>POST /verification/send</c>: emite o OTP usado em payouts fiat.</summary>
public sealed record SendVerificationCodeRequest
{
    /// <summary>Identificador do cliente.</summary>
    public string? ClientUuid { get; init; }

    /// <summary>E-mail que receberá o código de 6 dígitos.</summary>
    public string? Email { get; init; }
}

/// <summary>Resposta de <c>POST /verification/send</c>.</summary>
public sealed record SendVerificationCodeResponse : KiraResponse
{
    /// <summary>Se o código foi enviado.</summary>
    public bool? Sent { get; init; }
}
