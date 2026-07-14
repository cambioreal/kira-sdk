using System.Text.Json.Serialization;
using CambioReal.Kira.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/recipients</c>. Exige chave de idempotência.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14 para o rail <see cref="Models.AccountType.Wallet"/>
/// (criado e verificado ponta a ponta): os dados bancários/wallet vão dentro de
/// <see cref="Account"/>, não soltos no corpo — o request antigo (campos flat, sem <c>account</c>)
/// sempre falhava com <c>400 account: Required</c>. Nome e sobrenome (ou razão social) ficam na
/// raiz, não em um único <c>account_holder_name</c>.
/// <para>
/// Para rails que não são <see cref="Models.AccountType.Wallet"/>, só os campos <b>obrigatórios</b>
/// de SWIFT foram confirmados (<see cref="RecipientAccount.AccountNumber"/>,
/// <see cref="RecipientAccount.SwiftCode"/>, <see cref="RecipientAccount.BankName"/>,
/// <see cref="RecipientAccount.BankAddress"/> — o último não existia no modelo anterior). Os
/// demais campos bancários/documento permanecem como um superconjunto por inferência (mesmo
/// padrão dos outros rails na doc em prosa), não confirmados individualmente contra o sandbox.
/// Campos desconhecidos devolvidos pela API caem em <see cref="KiraResponse.AdditionalData"/>.
/// </para>
/// </remarks>
public sealed record CreateRecipientRequest
{
    /// <summary>Usuário dono do recipient.</summary>
    public required string UserId { get; init; }

    /// <summary>Nome do beneficiário, para pessoa física.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome do beneficiário, para pessoa física.</summary>
    public string? LastName { get; init; }

    /// <summary>Razão social do beneficiário, para pessoa jurídica.</summary>
    public string? CompanyName { get; init; }

    /// <summary>E-mail do beneficiário.</summary>
    public string? Email { get; init; }

    /// <summary>Telefone do beneficiário.</summary>
    public string? Phone { get; init; }

    /// <summary>Dados bancários/wallet. O discriminador é <see cref="RecipientAccount.AccountType"/>.</summary>
    public required RecipientAccount Account { get; init; }
}

/// <summary>
/// Dados de conta de um recipient — o payload varia por <see cref="AccountType"/>, mas a Kira
/// não publica o schema por rail. Preencha o que o rail exige.
/// </summary>
public sealed record RecipientAccount
{
    /// <summary>Rail de pagamento. Discrimina quais dos demais campos são exigidos.</summary>
    public required AccountType AccountType { get; init; }

    /// <summary>Endereço da carteira. Confirmado para <see cref="Models.AccountType.Wallet"/>.</summary>
    public string? Address { get; init; }

    /// <summary>Blockchain. Confirmado para <see cref="Models.AccountType.Wallet"/>.</summary>
    public WalletNetwork? Network { get; init; }

    /// <summary>Stablecoin. Confirmado para <see cref="Models.AccountType.Wallet"/>.</summary>
    public WalletToken? Token { get; init; }

    /// <summary>Nome do banco. Confirmado obrigatório para <see cref="Models.AccountType.Swift"/>.</summary>
    public string? BankName { get; init; }

    /// <summary>Endereço do banco. Confirmado obrigatório para <see cref="Models.AccountType.Swift"/> — ausente do modelo anterior.</summary>
    public string? BankAddress { get; init; }

    /// <summary>Código do banco. Para PSE, obtenha via <c>GET /banks</c>. Não confirmado individualmente.</summary>
    public string? BankCode { get; init; }

    /// <summary>Número da conta. Confirmado obrigatório para <see cref="Models.AccountType.Swift"/>.</summary>
    public string? AccountNumber { get; init; }

    /// <summary>Routing number (ACH e WIRE domésticos). Não confirmado individualmente.</summary>
    public string? RoutingNumber { get; init; }

    /// <summary>BIC/SWIFT. Confirmado obrigatório para <see cref="Models.AccountType.Swift"/>.</summary>
    public string? SwiftCode { get; init; }

    /// <summary>IBAN. Não confirmado individualmente.</summary>
    public string? Iban { get; init; }

    /// <summary>CLABE (SPEI, México). Não confirmado individualmente.</summary>
    public string? Clabe { get; init; }

    /// <summary>Tipo do documento do beneficiário (rails LATAM). Não confirmado individualmente.</summary>
    public string? DocumentType { get; init; }

    /// <summary>Número do documento do beneficiário (rails LATAM). Não confirmado individualmente.</summary>
    public string? DocumentNumber { get; init; }
}

/// <summary>
/// Recipient devolvido pela Kira. Confirmado idêntico em <c>POST</c> e <c>GET /v1/recipients/{id}</c>.
/// </summary>
public sealed record KiraRecipient : KiraResponse
{
    /// <summary>Identificador do recipient. Campo real é <c>recipient_id</c>, não <c>id</c>.</summary>
    [JsonPropertyName("recipient_id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Usuário dono. Não confirmado presente na resposta — <c>user_id</c> não apareceu em nenhum
    /// teste de criação/consulta contra o sandbox; mantido nulo por padrão.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>Pessoa física ou jurídica. Valores observados: <c>individual</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Nome do beneficiário, para pessoa física.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome do beneficiário, para pessoa física.</summary>
    public string? LastName { get; init; }

    /// <summary>Razão social do beneficiário, para pessoa jurídica.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Rail de pagamento.</summary>
    public AccountType? AccountType { get; init; }

    /// <summary>Dados da conta — a forma varia por <see cref="AccountType"/>, como no request.</summary>
    public RecipientAccountDetails? AccountDetails { get; init; }

    /// <summary>
    /// Criação. Campo real é <c>created_ts</c>, não <c>created_at</c>, e em um formato que o
    /// conversor padrão de <see cref="DateTimeOffset"/> rejeita — ver <see cref="KiraTimestampConverter"/>.
    /// </summary>
    [JsonPropertyName("created_ts")]
    [JsonConverter(typeof(KiraTimestampConverter))]
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Última atualização. Campo real é <c>updated_ts</c>, não <c>updated_at</c>.</summary>
    [JsonPropertyName("updated_ts")]
    [JsonConverter(typeof(KiraTimestampConverter))]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>Dados da conta de um <see cref="KiraRecipient"/>. Confirmado para o rail <c>WALLET</c>.</summary>
public sealed record RecipientAccountDetails : KiraResponse
{
    /// <summary>Endereço da carteira, para recipients cripto.</summary>
    public string? Address { get; init; }

    /// <summary>Blockchain, para recipients cripto.</summary>
    public WalletNetwork? Network { get; init; }

    /// <summary>Stablecoin, para recipients cripto.</summary>
    public WalletToken? Token { get; init; }
}

/// <summary>
/// Envelope real de <c>GET /v1/recipients?user_id=</c>: <c>{"recipients": [...], "total": N}</c>.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14 — nem array solto na raiz, nem o envelope
/// <c>{data, pagination}</c> usado por <c>GET /v1/users</c> e <c>GET /v1/virtual-accounts</c>.
/// Mais um formato de envelope distinto, por família de endpoint.
/// </remarks>
internal sealed record KiraRecipientsEnvelope(IReadOnlyList<KiraRecipient> Recipients, int? Total);
