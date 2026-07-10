using CambioReal.Kira.Http;
using CambioReal.Kira.Models;

namespace CambioReal.Kira.Resources;

/// <summary>Contas virtuais, depósitos, payouts e endereços de liquidação.</summary>
public sealed class VirtualAccountsResource
{
    private readonly KiraClient client;

    internal VirtualAccountsResource(KiraClient client) => this.client = client;

    /// <summary>Lista contas virtuais. <c>GET /v1/virtual-accounts</c>.</summary>
    public Task<KiraPage<KiraVirtualAccount>> ListAsync(
        ListVirtualAccountsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        request ??= new ListVirtualAccountsRequest();

        var path = QueryString.Append(
            KiraPaths.VirtualAccounts,
            ("status", QueryString.Format(request.Status)),
            ("user_id", request.UserId),
            ("type", request.Type),
            ("mode", QueryString.Format(request.Mode)),
            ("search", request.Search),
            ("page", QueryString.Format(request.Page)),
            ("limit", QueryString.Format(request.Limit)));

        return client.GetAsync<KiraPage<KiraVirtualAccount>>(path, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Cria uma conta virtual para um usuário. <c>POST /v1/users/{id}/virtual-accounts</c>.
    /// </summary>
    /// <remarks>
    /// Informar <see cref="CreateVirtualAccountRequest.Destination"/> cria em modo cripto; omiti-lo,
    /// em modo fiat. O modo é imutável depois da criação. O usuário precisa estar verificado.
    /// </remarks>
    public Task<KiraVirtualAccount> CreateAsync(
        string userId,
        CreateVirtualAccountRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateVirtualAccountRequest, KiraVirtualAccount>(
            KiraPaths.UserVirtualAccounts(userId),
            request,
            UsersResource.Idempotent(idempotencyKey),
            cancellationToken);

    /// <summary>Busca uma conta virtual. <c>GET /v1/virtual-accounts/{id}</c>.</summary>
    public Task<KiraVirtualAccount> GetAsync(string virtualAccountId, CancellationToken cancellationToken = default) =>
        client.GetAsync<KiraVirtualAccount>(KiraPaths.VirtualAccount(virtualAccountId), cancellationToken: cancellationToken);

    /// <summary>Lista as contas virtuais de um usuário. <c>GET /v1/users/{id}/virtual-accounts</c>.</summary>
    public Task<IReadOnlyList<KiraVirtualAccount>> ListByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        client.GetAsync<IReadOnlyList<KiraVirtualAccount>>(KiraPaths.UserVirtualAccounts(userId), cancellationToken: cancellationToken);

    /// <summary>
    /// Saldo de uma conta virtual. <c>GET /v1/virtual-accounts/{id}/balance</c>.
    /// </summary>
    /// <remarks>Só funciona em contas fiat. Contas cripto convertem e enviam, não acumulam saldo.</remarks>
    public Task<VirtualAccountBalance> GetBalanceAsync(string virtualAccountId, CancellationToken cancellationToken = default) =>
        client.GetAsync<VirtualAccountBalance>(KiraPaths.VirtualAccountBalance(virtualAccountId), cancellationToken: cancellationToken);

    /// <summary>Lista os depósitos de uma conta virtual. <c>GET /v1/virtual-accounts/{id}/deposits</c>.</summary>
    public Task<IReadOnlyList<KiraDeposit>> ListDepositsAsync(string virtualAccountId, CancellationToken cancellationToken = default) =>
        client.GetAsync<IReadOnlyList<KiraDeposit>>(KiraPaths.VirtualAccountDeposits(virtualAccountId), cancellationToken: cancellationToken);

    /// <summary>Busca um depósito. <c>GET /v1/virtual-accounts/{id}/deposits/{depositId}</c>.</summary>
    public Task<KiraDeposit> GetDepositAsync(
        string virtualAccountId,
        string depositId,
        CancellationToken cancellationToken = default) =>
        client.GetAsync<KiraDeposit>(
            KiraPaths.VirtualAccountDeposit(virtualAccountId, depositId),
            cancellationToken: cancellationToken);

    /// <summary>
    /// Calcula as taxas de um payout sem criar nada. <c>POST /v1/virtual-accounts/{id}/payout/preview</c>.
    /// </summary>
    /// <remarks>
    /// Chame isto antes de <see cref="InitiatePayoutAsync"/> para mostrar as taxas ao usuário.
    /// Com <see cref="PreviewPayoutRequest.CreateQuote"/> a resposta também trava a cotação.
    /// </remarks>
    public Task<PreviewPayoutResponse> PreviewPayoutAsync(
        string virtualAccountId,
        PreviewPayoutRequest request,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<PreviewPayoutRequest, PreviewPayoutResponse>(
            KiraPaths.VirtualAccountPayoutPreview(virtualAccountId),
            request,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Inicia um payout. <c>POST /v1/virtual-accounts/{id}/payout</c>.
    /// </summary>
    /// <remarks>
    /// Payout <b>fiat</b> (sem <see cref="InitiatePayoutRequest.PaymentInstructions"/>) exige
    /// <paramref name="otpCode"/>: um código de 6 dígitos obtido em
    /// <see cref="PlatformResource.SendVerificationCodeAsync"/>. Omiti-lo produz
    /// <c>400 VALIDATION_REQUIRED</c>.
    /// <para>
    /// Payout <b>cripto</b> (com <c>payment_instructions</c>) dispensa OTP mas exige
    /// <see cref="InitiatePayoutRequest.SupportingDocuments"/>, e a resposta traz o endereço de
    /// depósito de uso único em <see cref="InitiatePayoutResponse.DepositInstructions"/>.
    /// </para>
    /// </remarks>
    /// <param name="virtualAccountId">Conta virtual de origem. Precisa ser <c>US_BANK</c> e estar ativa.</param>
    /// <param name="request">Dados do payout.</param>
    /// <param name="otpCode">Código OTP de 6 dígitos. Obrigatório no modo fiat.</param>
    /// <param name="idempotencyKey">Sua chave de idempotência.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public Task<InitiatePayoutResponse> InitiatePayoutAsync(
        string virtualAccountId,
        InitiatePayoutRequest request,
        string? otpCode = null,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        var context = new KiraRequestContext
        {
            IdempotencyKey = idempotencyKey ?? Guid.NewGuid(),
            ValidationCode = otpCode,
        };

        return client.PostAsync<InitiatePayoutRequest, InitiatePayoutResponse>(
            KiraPaths.VirtualAccountPayout(virtualAccountId),
            request,
            context,
            cancellationToken);
    }

    /// <summary>
    /// Cria o endereço de liquidação. <c>POST /v1/virtual-accounts/{id}/liquidation-address</c>.
    /// </summary>
    /// <remarks>Depósitos cripto nesse endereço viram USD e seguem para o recipient configurado.</remarks>
    public Task<LiquidationAddress> CreateLiquidationAddressAsync(
        string virtualAccountId,
        CreateLiquidationAddressRequest request,
        Guid? idempotencyKey = null,
        CancellationToken cancellationToken = default) =>
        client.PostAsync<CreateLiquidationAddressRequest, LiquidationAddress>(
            KiraPaths.VirtualAccountLiquidationAddress(virtualAccountId),
            request,
            UsersResource.Idempotent(idempotencyKey),
            cancellationToken);

    /// <summary>Busca o endereço de liquidação. <c>GET /v1/virtual-accounts/{id}/liquidation-address</c>.</summary>
    public Task<LiquidationAddress> GetLiquidationAddressAsync(
        string virtualAccountId,
        CancellationToken cancellationToken = default) =>
        client.GetAsync<LiquidationAddress>(
            KiraPaths.VirtualAccountLiquidationAddress(virtualAccountId),
            cancellationToken: cancellationToken);
}
