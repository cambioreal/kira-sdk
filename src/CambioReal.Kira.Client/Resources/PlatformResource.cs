using CambioReal.Kira.Http;
using CambioReal.Kira.Models;

namespace CambioReal.Kira.Resources;

/// <summary>Coleta de pagamentos via PSE (Colômbia) e SPEI (México).</summary>
public sealed class PayInsResource
{
    private readonly KiraClient client;

    internal PayInsResource(KiraClient client) => this.client = client;

    /// <summary>
    /// Cria um PayIn e gera o link de pagamento. <c>POST /v1/payins</c>.
    /// </summary>
    /// <remarks>
    /// PSE é de uso único e exige valor e banco. SPEI é reutilizável e não aceita valor — o pagador
    /// escolhe quanto pagar, e cada pagamento vira um elemento de <see cref="KiraPayIn.Settlement"/>.
    /// </remarks>
    public Task<KiraPayIn> CreateAsync(
        CreatePayInRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreatePayInRequest, KiraPayIn>(
            KiraPaths.PayIns,
            request,
            UsersResource.Idempotent(idempotencyKey),
            cancellationToken);

    /// <summary>
    /// Calcula as taxas antes de criar o PayIn. <c>POST /v1/payins/fees</c>.
    /// </summary>
    /// <remarks>A cotação vale 10 minutos. Depois disso, peça uma nova.</remarks>
    public Task<PayInFeesResponse> CalculateFeesAsync(
        PayInFeesRequest request,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<PayInFeesRequest, PayInFeesResponse>(
            KiraPaths.PayInFees,
            request,
            cancellationToken: cancellationToken);

    /// <summary>Busca um PayIn e suas liquidações. <c>GET /v1/payins/{id}</c>.</summary>
    public Task<KiraPayIn> GetAsync(string payInId, CancellationToken cancellationToken = default) =>
        client.GetAsync<KiraPayIn>(KiraPaths.PayIn(payInId), cancellationToken: cancellationToken);
}

/// <summary>Payment links.</summary>
public sealed class PaymentLinksResource
{
    private readonly KiraClient client;

    internal PaymentLinksResource(KiraClient client) => this.client = client;

    /// <summary>Cria um payment link. <c>POST /v1/payment-link</c>.</summary>
    public Task<KiraPaymentLink> CreateAsync(
        CreatePaymentLinkRequest request,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreatePaymentLinkRequest, KiraPaymentLink>(
            KiraPaths.PaymentLink,
            request,
            cancellationToken: cancellationToken);
}

/// <summary>Webhooks, dados de referência e emissão de OTP.</summary>
public sealed class PlatformResource
{
    private readonly KiraClient client;

    internal PlatformResource(KiraClient client) => this.client = client;

    /// <summary>
    /// Registra a URL que receberá os eventos. <c>POST /webhooks/register</c>.
    /// </summary>
    /// <remarks>
    /// Único endpoint que autentica <b>só</b> com <c>x-api-key</c>, sem JWT. A URL precisa ser HTTPS.
    /// Guarde o <see cref="RegisterWebhookRequest.SecretKey"/> no <c>pass</c>: sem ele, não há como
    /// distinguir um evento da Kira de um POST forjado.
    /// <para>
    /// Não há endpoint documentado para listar ou remover webhooks.
    /// </para>
    /// </remarks>
    public Task<KiraWebhookRegistration> RegisterWebhookAsync(
        RegisterWebhookRequest request,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<RegisterWebhookRequest, KiraWebhookRegistration>(
            KiraPaths.WebhooksRegister,
            request,
            new KiraRequestContext { SkipBearerAuthentication = true },
            cancellationToken);

    /// <summary>
    /// Emite o OTP de 6 dígitos usado em payouts fiat. <c>POST /verification/send</c>.
    /// </summary>
    /// <remarks>
    /// Passe o código devolvido por e-mail em <see cref="VirtualAccountsResource.InitiatePayoutAsync"/>.
    /// Tentativas repetidas com código errado bloqueiam a verificação temporariamente.
    /// </remarks>
    public Task<SendVerificationCodeResponse> SendVerificationCodeAsync(
        SendVerificationCodeRequest request,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<SendVerificationCodeRequest, SendVerificationCodeResponse>(
            KiraPaths.VerificationSend,
            request,
            cancellationToken: cancellationToken);

    /// <summary>Países suportados e suas subdivisões. <c>GET /v1/countries</c>.</summary>
    public async Task<IReadOnlyList<KiraCountry>> ListCountriesAsync(CancellationToken cancellationToken = default)
    {
        var envelope = await client.GetAsync<KiraCountriesEnvelope>(KiraPaths.Countries, cancellationToken: cancellationToken);
        return envelope.Data;
    }

    /// <summary>
    /// Bancos de um país. <c>GET /banks?country=</c>.
    /// </summary>
    /// <remarks>Use o <see cref="KiraBank.Code"/> em <see cref="CreatePayInRequest.BankCode"/>.</remarks>
    public Task<IReadOnlyList<KiraBank>> ListBanksAsync(string countryCode, CancellationToken cancellationToken = default)
    {
        var path = QueryString.Append(KiraPaths.Banks, ("country", countryCode));
        return client.GetAsync<IReadOnlyList<KiraBank>>(path, cancellationToken: cancellationToken);
    }
}
