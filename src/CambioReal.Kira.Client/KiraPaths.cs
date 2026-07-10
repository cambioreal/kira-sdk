namespace CambioReal.Kira;

/// <summary>
/// Todos os paths da API Kira, em um só lugar.
/// </summary>
/// <remarks>
/// A documentação da Kira se contradiz sobre alguns paths. Centralizá-los aqui torna cada
/// contradição uma correção de uma linha, em vez de uma caça pelo código. Os conflitos
/// conhecidos estão anotados no próprio membro.
/// <para>
/// Todos os paths são relativos e sem barra inicial — ver <see cref="KiraEnvironmentExtensions.GetBaseAddress"/>.
/// </para>
/// </remarks>
public static class KiraPaths
{
    /// <summary>Autenticação. Único endpoint que dispensa o bearer.</summary>
    public const string Auth = "auth";

    /// <summary>Emite o OTP de 6 dígitos usado em payouts fiat.</summary>
    /// <remarks>Citado no guia de Payouts; ausente da API Reference. Path inferido.</remarks>
    public const string VerificationSend = "verification/send";

    /// <summary>Coleção de usuários. <c>POST</c> cria, <c>GET</c> lista.</summary>
    public const string Users = "v1/users";

    /// <summary>Países suportados e suas subdivisões.</summary>
    public const string Countries = "v1/countries";

    /// <summary>Bancos por país. Espera <c>?country=</c>.</summary>
    /// <remarks>A doc não declara o nome do parâmetro de query; <c>country</c> é inferido.</remarks>
    public const string Banks = "banks";

    /// <summary>Coleção de recipients.</summary>
    public const string Recipients = "v1/recipients";

    /// <summary>Coleção de contas virtuais.</summary>
    public const string VirtualAccounts = "v1/virtual-accounts";

    /// <summary>Criação de payment link.</summary>
    public const string PaymentLink = "v1/payment-link";

    /// <summary>Coleção de PayIns.</summary>
    public const string PayIns = "v1/payins";

    /// <summary>Cálculo de taxas de PayIn.</summary>
    public const string PayInFees = "v1/payins/fees";

    /// <summary>Registro de webhook. Autentica só com <c>x-api-key</c>.</summary>
    public const string WebhooksRegister = "webhooks/register";

    /// <summary>Usuário por id.</summary>
    public static string User(string userId) => $"{Users}/{Escape(userId)}";

    /// <summary>Verificação manual (fluxo legado).</summary>
    public static string UserVerifications(string userId) => $"{User(userId)}/verifications";

    /// <summary>Contas virtuais de um usuário.</summary>
    public static string UserVirtualAccounts(string userId) => $"{User(userId)}/virtual-accounts";

    /// <summary>Recipient por id.</summary>
    public static string Recipient(string recipientId) => $"{Recipients}/{Escape(recipientId)}";

    /// <summary>Conta virtual por id.</summary>
    public static string VirtualAccount(string virtualAccountId) => $"{VirtualAccounts}/{Escape(virtualAccountId)}";

    /// <summary>Saldo de uma conta virtual. Só existe em contas fiat.</summary>
    public static string VirtualAccountBalance(string virtualAccountId) => $"{VirtualAccount(virtualAccountId)}/balance";

    /// <summary>Depósitos de uma conta virtual.</summary>
    public static string VirtualAccountDeposits(string virtualAccountId) => $"{VirtualAccount(virtualAccountId)}/deposits";

    /// <summary>Depósito específico de uma conta virtual.</summary>
    public static string VirtualAccountDeposit(string virtualAccountId, string depositId) =>
        $"{VirtualAccountDeposits(virtualAccountId)}/{Escape(depositId)}";

    /// <summary>Endereço de liquidação (carteira cripto) de uma conta virtual.</summary>
    public static string VirtualAccountLiquidationAddress(string virtualAccountId) =>
        $"{VirtualAccount(virtualAccountId)}/liquidation-address";

    /// <summary>
    /// Inicia um payout a partir de uma conta virtual.
    /// </summary>
    /// <remarks>
    /// <b>Contradição conhecida.</b> O guia de Payouts e a API Reference concordam neste path
    /// (o payout parte de uma conta virtual). A página de Idempotency, isolada, cita
    /// <c>/v1/payouts</c> e <c>/v1/batch-payouts</c>. Seguimos a maioria; se o sandbox provar o
    /// contrário, a correção é aqui.
    /// </remarks>
    public static string VirtualAccountPayout(string virtualAccountId) => $"{VirtualAccount(virtualAccountId)}/payout";

    /// <summary>Prévia de taxas de um payout, sem criar nada.</summary>
    public static string VirtualAccountPayoutPreview(string virtualAccountId) => $"{VirtualAccountPayout(virtualAccountId)}/preview";

    /// <summary>PayIn por id.</summary>
    public static string PayIn(string payInId) => $"{PayIns}/{Escape(payInId)}";

    private static string Escape(string segment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment);
        return Uri.EscapeDataString(segment);
    }
}
