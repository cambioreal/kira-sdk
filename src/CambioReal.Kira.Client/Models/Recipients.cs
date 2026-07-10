namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/recipients</c>. Exige chave de idempotência.
/// </summary>
/// <remarks>
/// Os campos bancários variam por <see cref="AccountType"/> e a Kira não publica o schema por rail.
/// Este é um superconjunto: preencha o que o rail exige. Campos desconhecidos devolvidos pela API
/// caem em <see cref="KiraResponse.AdditionalData"/>.
/// <para>
/// Desde 2026-04-14, <see cref="Email"/>, <see cref="Phone"/> e os campos de endereço são
/// opcionais para recipients SWIFT.
/// </para>
/// </remarks>
public sealed record CreateRecipientRequest
{
    /// <summary>Usuário dono do recipient.</summary>
    public required string UserId { get; init; }

    /// <summary>Rail de pagamento.</summary>
    public required AccountType AccountType { get; init; }

    /// <summary>Moeda de destino.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Nome do beneficiário.</summary>
    public string? AccountHolderName { get; init; }

    /// <summary>E-mail do beneficiário.</summary>
    public string? Email { get; init; }

    /// <summary>Telefone do beneficiário.</summary>
    public string? Phone { get; init; }

    /// <summary>Nome do banco.</summary>
    public string? BankName { get; init; }

    /// <summary>Código do banco. Para PSE, obtenha via <c>GET /banks</c>.</summary>
    public string? BankCode { get; init; }

    /// <summary>Número da conta.</summary>
    public string? AccountNumber { get; init; }

    /// <summary>Routing number (ACH e WIRE domésticos).</summary>
    public string? RoutingNumber { get; init; }

    /// <summary>BIC/SWIFT.</summary>
    public string? SwiftCode { get; init; }

    /// <summary>IBAN.</summary>
    public string? Iban { get; init; }

    /// <summary>CLABE (SPEI, México).</summary>
    public string? Clabe { get; init; }

    /// <summary>Tipo do documento do beneficiário (rails LATAM).</summary>
    public string? DocumentType { get; init; }

    /// <summary>Número do documento do beneficiário (rails LATAM).</summary>
    public string? DocumentNumber { get; init; }

    /// <summary>Endereço da carteira, quando <see cref="AccountType"/> é <see cref="AccountType.Wallet"/>.</summary>
    public string? WalletAddress { get; init; }

    /// <summary>Blockchain da carteira.</summary>
    public WalletNetwork? WalletNetwork { get; init; }

    /// <summary>Stablecoin da carteira.</summary>
    public WalletToken? WalletToken { get; init; }

    /// <summary>Logradouro.</summary>
    public string? AddressStreet { get; init; }

    /// <summary>Cidade.</summary>
    public string? AddressCity { get; init; }

    /// <summary>Estado.</summary>
    public string? AddressState { get; init; }

    /// <summary>CEP.</summary>
    public string? AddressZipCode { get; init; }

    /// <summary>País (ISO 3166-1 alpha-3).</summary>
    public string? AddressCountry { get; init; }
}

/// <summary>Recipient devolvido pela Kira.</summary>
public sealed record KiraRecipient : KiraResponse
{
    /// <summary>Identificador do recipient.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Usuário dono.</summary>
    public string? UserId { get; init; }

    /// <summary>Rail de pagamento.</summary>
    public AccountType? AccountType { get; init; }

    /// <summary>Moeda de destino.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Nome do beneficiário.</summary>
    public string? AccountHolderName { get; init; }

    /// <summary>Nome do banco.</summary>
    public string? BankName { get; init; }

    /// <summary>Endereço da carteira, para recipients cripto.</summary>
    public string? WalletAddress { get; init; }

    /// <summary>Blockchain da carteira.</summary>
    public WalletNetwork? WalletNetwork { get; init; }

    /// <summary>Stablecoin da carteira.</summary>
    public WalletToken? WalletToken { get; init; }

    /// <summary>Criação.</summary>
    public DateTimeOffset? CreatedAt { get; init; }
}
