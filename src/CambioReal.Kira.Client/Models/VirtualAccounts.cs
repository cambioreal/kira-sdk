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
/// <b>Reescrito em 2026-07-14</b> a partir de erros de validação reais do sandbox — não foi
/// possível chegar a um <c>201</c> completo (o usuário de teste não reúne KYC suficiente para
/// virar "customer" do provedor: <c>"Customer not found... A customer is required for US_ACH
/// accounts."</c>), mas o formato do corpo em si está confirmado: <see cref="Type"/> e
/// <see cref="Destination"/> são <b>obrigatórios</b>, contra a suposição anterior de que
/// omitir o destino bastava para o modo fiat. Não confirmado: a forma de <see cref="Destination"/>
/// para os tipos que não são um destino cripto (ex.: se <c>MX_SPEI</c>/<c>EU_SEPA</c> aceitam um
/// IBAN/CLABE em <see cref="VirtualAccountDestination.Address"/> em vez de um endereço de
/// carteira) — só foi testado com um destino de carteira Solana/USDC contra <see cref="VirtualAccountType.UsAch"/>.
/// </remarks>
public sealed record CreateVirtualAccountRequest
{
    /// <summary>
    /// Tipo/rail da conta. Confirmado obrigatório contra o sandbox em 2026-07-14 — substitui a
    /// suposição anterior de string livre (<c>US_BANK</c> não é aceito).
    /// </summary>
    public required VirtualAccountType Type { get; init; }

    /// <summary>
    /// Destino dos depósitos. Confirmado obrigatório contra o sandbox em 2026-07-14, inclusive
    /// para <see cref="VirtualAccountType.UsAch"/> — contradiz a doc em prosa, que descrevia isto
    /// como opcional/exclusivo do modo cripto.
    /// </summary>
    public required VirtualAccountDestination Destination { get; init; }

    /// <summary>
    /// Banco que provisiona a conta. <see cref="VirtualAccountProvider.SlovakSavingsBank"/> só
    /// existe em sandbox — que, por sua vez, só oferece esse provedor.
    /// </summary>
    public VirtualAccountProvider? Provider { get; init; }

    /// <summary>
    /// Modo. Não confirmado se ainda é aceito/relevante junto de <see cref="Type"/> — mantido por
    /// não ter sido testado como campo desconhecido/rejeitado.
    /// </summary>
    public VirtualAccountMode? Mode { get; init; }

    /// <summary>Markup do cliente sobre a conversão, em pontos percentuais.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public decimal? Markup { get; init; }

    /// <summary>Descrição livre.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// Destino dos depósitos de uma conta virtual, exigido em <see cref="CreateVirtualAccountRequest"/>.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14 via erro de validação: os campos são
/// <c>currency</c>/<c>network</c>/<c>address</c> — não <c>token</c>/<c>network</c>/<c>address</c>
/// como em <see cref="CryptoDestination"/> (usado em endereços de liquidação e payouts cripto,
/// não alterado aqui por não ter sido sondado contra este endpoint especificamente). Testado com
/// sucesso na camada de schema (passou da validação de formato para uma regra de negócio) usando
/// <c>currency=USDC</c>.
/// </remarks>
public sealed record VirtualAccountDestination
{
    /// <summary>Moeda/stablecoin do destino.</summary>
    public required Currency Currency { get; init; }

    /// <summary>Blockchain do destino.</summary>
    public required WalletNetwork Network { get; init; }

    /// <summary>Endereço do destino.</summary>
    public required string Address { get; init; }
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
