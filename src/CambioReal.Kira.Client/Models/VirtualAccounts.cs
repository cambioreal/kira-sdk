using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>Carteira de destino de uma conta virtual em modo cripto.</summary>
public sealed record CryptoDestination
{
    /// <summary>Blockchain.</summary>
    public required WalletNetwork Network { get; init; }

    /// <summary>Stablecoin.</summary>
    public required WalletToken Token { get; init; }

    /// <summary>Endereço da carteira.</summary>
    public required string Address { get; init; }
}

/// <summary>
/// Corpo de <c>POST /v1/users/{id}/virtual-accounts</c>. Exige chave de idempotência.
/// </summary>
/// <remarks>
/// Informar <see cref="Destination"/> cria a conta em modo cripto (depósitos são convertidos em
/// stablecoin); omiti-lo cria em modo fiat (mantém saldo em USD). <b>O modo é imutável.</b>
/// </remarks>
public sealed record CreateVirtualAccountRequest
{
    /// <summary>
    /// Banco que provisiona a conta. <see cref="VirtualAccountProvider.SlovakSavingsBank"/> só
    /// existe em sandbox — que, por sua vez, só oferece esse provedor.
    /// </summary>
    public VirtualAccountProvider? Provider { get; init; }

    /// <summary>Modo. Redundante com a presença de <see cref="Destination"/>; a Kira valida a coerência.</summary>
    public VirtualAccountMode? Mode { get; init; }

    /// <summary>Carteira de destino. Presente ⇒ modo cripto.</summary>
    public CryptoDestination? Destination { get; init; }

    /// <summary>Markup do cliente sobre a conversão, em pontos percentuais.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public decimal? Markup { get; init; }

    /// <summary>Descrição livre.</summary>
    public string? Description { get; init; }
}

/// <summary>Instruções para depositar em uma conta virtual.</summary>
public sealed record DepositInstructions : KiraResponse
{
    /// <summary>Nome do banco recebedor.</summary>
    public string? BankName { get; init; }

    /// <summary>Número da conta.</summary>
    public string? AccountNumber { get; init; }

    /// <summary>Routing number.</summary>
    public string? RoutingNumber { get; init; }

    /// <summary>Nome do beneficiário.</summary>
    public string? BeneficiaryName { get; init; }

    /// <summary>Endereço do beneficiário.</summary>
    public string? BeneficiaryAddress { get; init; }

    /// <summary>Endereço do banco.</summary>
    public string? BankAddress { get; init; }
}

/// <summary>Conta virtual devolvida pela Kira.</summary>
public sealed record KiraVirtualAccount : KiraResponse
{
    /// <summary>Identificador da conta.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Usuário dono.</summary>
    public string? UserId { get; init; }

    /// <summary>Ciclo de vida. Payouts exigem <see cref="VirtualAccountStatus.Active"/>.</summary>
    public VirtualAccountStatus? Status { get; init; }

    /// <summary>Fiat ou cripto.</summary>
    public VirtualAccountMode? Mode { get; init; }

    /// <summary>Banco provedor.</summary>
    public VirtualAccountProvider? Provider { get; init; }

    /// <summary>Tipo da conta. Payouts exigem <c>US_BANK</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Instruções de depósito.</summary>
    public DepositInstructions? DepositInstructions { get; init; }

    /// <summary>Carteira de destino, em modo cripto.</summary>
    public CryptoDestination? Destination { get; init; }

    /// <summary>Descrição livre.</summary>
    public string? Description { get; init; }

    /// <summary>Criação.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Saldo de uma conta virtual. Só existe em contas fiat.</summary>
public sealed record VirtualAccountBalance : KiraResponse
{
    /// <summary>Saldo disponível.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; init; }

    /// <summary>Moeda do saldo.</summary>
    public Currency? Currency { get; init; }
}

/// <summary>Filtros de <c>GET /v1/virtual-accounts</c>.</summary>
public sealed record ListVirtualAccountsRequest
{
    /// <summary>Situação.</summary>
    public VirtualAccountStatus? Status { get; init; }

    /// <summary>Usuário dono.</summary>
    public string? UserId { get; init; }

    /// <summary>Tipo da conta.</summary>
    public string? Type { get; init; }

    /// <summary>Fiat ou cripto.</summary>
    public VirtualAccountMode? Mode { get; init; }

    /// <summary>Busca textual livre.</summary>
    public string? Search { get; init; }

    /// <summary>Página, base 1.</summary>
    public int? Page { get; init; }

    /// <summary>Itens por página.</summary>
    public int? Limit { get; init; }
}

/// <summary>Depósito recebido por uma conta virtual.</summary>
public sealed record KiraDeposit : KiraResponse
{
    /// <summary>Identificador do depósito.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Valor depositado.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Amount { get; init; }

    /// <summary>Moeda do depósito.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Situação.</summary>
    public string? Status { get; init; }

    /// <summary>Rail pelo qual o dinheiro chegou (ACH, WIRE, …).</summary>
    public string? PaymentRail { get; init; }

    /// <summary>Nome do remetente.</summary>
    public string? SenderName { get; init; }

    /// <summary>Hash da transação de liquidação, quando a conta é cripto.</summary>
    public string? SettlementTransactionHash { get; init; }

    /// <summary>Criação.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Corpo de <c>POST /v1/virtual-accounts/{id}/liquidation-address</c>.</summary>
/// <remarks>Depósitos cripto neste endereço são convertidos em USD e enviados ao recipient configurado.</remarks>
public sealed record CreateLiquidationAddressRequest
{
    /// <summary>Blockchain. USDT aceita Tron; USDC não.</summary>
    public required WalletNetwork Network { get; init; }

    /// <summary>Stablecoin aceita no endereço.</summary>
    public required WalletToken Token { get; init; }

    /// <summary>Recipient que recebe os USD convertidos.</summary>
    public required string RecipientId { get; init; }
}

/// <summary>Endereço de liquidação de uma conta virtual.</summary>
public sealed record LiquidationAddress : KiraResponse
{
    /// <summary>Identificador.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Endereço da carteira que recebe os depósitos cripto.</summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>Blockchain.</summary>
    public WalletNetwork? Network { get; init; }

    /// <summary>Stablecoin.</summary>
    public WalletToken? Token { get; init; }

    /// <summary>Recipient que recebe os USD convertidos.</summary>
    public string? RecipientId { get; init; }
}
