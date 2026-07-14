using System.Text.Json.Serialization;
using CambioReal.Kira.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>Natureza do usuário.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<UserType>))]
public enum UserType
{
    /// <summary>Pessoa física. Dispara KYC.</summary>
    Individual,

    /// <summary>Pessoa jurídica. Dispara KYB.</summary>
    Business,
}

/// <summary>Como o KYC/KYB é coletado.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<VerificationMode>))]
public enum VerificationMode
{
    /// <summary>Payload completo; a Kira dispara a verificação em background.</summary>
    Automatic,

    /// <summary>Payload mínimo; a Kira devolve um link hospedado para o usuário final.</summary>
    VerificationLink,
}

/// <summary>Situação de verificação do usuário.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<VerificationStatus>))]
public enum VerificationStatus
{
    /// <summary>Ainda não verificado. Estado inicial após <c>user.created</c>.</summary>
    Unverified,

    /// <summary>Verificação em andamento.</summary>
    Pending,

    /// <summary>Verificado.</summary>
    Verified,

    /// <summary>Verificação recusada.</summary>
    Failed,
}

/// <summary>Profundidade do KYC concluído. A Kira devolve estes valores em PascalCase.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<KycType>))]
public enum KycType
{
    /// <summary>Verificação simplificada.</summary>
    Simplified,

    /// <summary>Verificação completa.</summary>
    Full,
}

/// <summary>
/// Produtos contra os quais a Kira avalia a elegibilidade do usuário.
/// A verificação dispara assim que <em>ao menos um</em> produto tem todos os campos exigidos.
/// </summary>
[JsonConverter(typeof(KebabCaseLowerEnumConverter<KiraProduct>))]
public enum KiraProduct
{
    /// <summary>Provedor Diameter. Exige imagens de documento e comprovantes.</summary>
    UsaVirtualAccounts,

    /// <summary>Provedor Austin Capital Trust. Sem fotos na criação; exige imigração, emprego e histórico bancário.</summary>
    UsaVirtualAccountsAct,

    /// <summary>
    /// Provedor Zenus. Não documentado — observado em <c>POST /v1/users</c> contra o sandbox em
    /// 2026-07-13, ao lado dos outros dois. Não modelado em <see cref="KiraEligibleProduct"/>
    /// (que usa <see cref="string"/> para tolerar produtos futuros sem quebrar a desserialização).
    /// </summary>
    UsaVirtualAccountsZenus,
}

/// <summary>Banco que provisiona a conta virtual.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<VirtualAccountProvider>))]
public enum VirtualAccountProvider
{
    /// <summary>Para usuários fora dos EUA.</summary>
    Portage,

    /// <summary>Para usuários nos EUA.</summary>
    AustinCapitalTrust,

    /// <summary>Somente sandbox.</summary>
    SlovakSavingsBank,
}

/// <summary>Modo da conta virtual. Definido na criação e imutável depois.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<VirtualAccountMode>))]
public enum VirtualAccountMode
{
    /// <summary>Mantém saldo em USD. Criada sem <c>destination</c>.</summary>
    Fiat,

    /// <summary>Converte depósitos em USDC/USDT e envia para uma carteira. Criada com <c>destination</c>.</summary>
    Crypto,
}

/// <summary>Ciclo de vida da conta virtual.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<VirtualAccountStatus>))]
public enum VirtualAccountStatus
{
    /// <summary>Criação iniciada.</summary>
    Pending,

    /// <summary>Provisionamento assíncrono em andamento.</summary>
    Activating,

    /// <summary>Pronta para receber depósitos.</summary>
    Active,

    /// <summary>Criação falhou.</summary>
    Failed,

    /// <summary>Conta encerrada.</summary>
    Deactivated,
}

/// <summary>
/// Rail de pagamento do recipient.
/// </summary>
/// <remarks>
/// <b>Contradição conhecida.</b> A API Reference do <c>initiatePayout</c> aceita apenas
/// <see cref="Wire"/> e <see cref="Swift"/>; o guia de Payouts inclui <see cref="Ach"/> e
/// <see cref="InstantPay"/>; o <c>previewPayout</c> aceita ACH, WIRE, SWIFT e WALLET.
/// Pior: <see cref="InstantPay"/> não aparece na lista de tipos aceitos ao criar um recipient,
/// então pode não haver como criar o destinatário que o guia diz suportar.
/// </remarks>
[JsonConverter(typeof(UpperSnakeCaseEnumConverter<AccountType>))]
public enum AccountType
{
    /// <summary>ACH doméstico (USD).</summary>
    Ach,

    /// <summary>Wire doméstico (USD).</summary>
    Wire,

    /// <summary>SWIFT internacional (USD).</summary>
    Swift,

    /// <summary>FedNow. Só funciona se o banco do beneficiário participar; validado na execução.</summary>
    InstantPay,

    /// <summary>SPEI (México, MXN).</summary>
    Spei,

    /// <summary>PSE (Colômbia, COP).</summary>
    Pse,

    /// <summary>Carteira cripto (USDC/USDT).</summary>
    Wallet,

    /// <summary>Argentina.</summary>
    Ars,

    /// <summary>Brasil.</summary>
    Brl,

    /// <summary>Chile.</summary>
    Clp,

    /// <summary>Costa Rica.</summary>
    Crc,

    /// <summary>República Dominicana.</summary>
    Dop,

    /// <summary>Equador (USD).</summary>
    Ecusd,

    /// <summary>Guatemala.</summary>
    Gtq,

    /// <summary>Panamá (USD).</summary>
    Pausd,

    /// <summary>Peru (PEN).</summary>
    Pen,

    /// <summary>Peru (USD).</summary>
    Peusd,

    /// <summary>Paraguai.</summary>
    Pyg,

    /// <summary>El Salvador (USD).</summary>
    Svusd,

    /// <summary>Uruguai.</summary>
    Uyu,
}

/// <summary>Blockchain de destino.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<WalletNetwork>))]
public enum WalletNetwork
{
    /// <summary>Solana. Suporta USDC e USDT.</summary>
    Solana,

    /// <summary>Polygon. Suporta USDC e USDT.</summary>
    Polygon,

    /// <summary>Tron. Suporta apenas USDT.</summary>
    Tron,
}

/// <summary>Stablecoin.</summary>
[JsonConverter(typeof(UpperSnakeCaseEnumConverter<WalletToken>))]
public enum WalletToken
{
    /// <summary>USD Coin.</summary>
    Usdc,

    /// <summary>Tether.</summary>
    Usdt,
}

/// <summary>Moeda. Inclui fiat e stablecoins, porque a Kira as trata no mesmo campo.</summary>
[JsonConverter(typeof(UpperSnakeCaseEnumConverter<Currency>))]
public enum Currency
{
    /// <summary>Dólar americano.</summary>
    Usd,

    /// <summary>Peso mexicano.</summary>
    Mxn,

    /// <summary>Peso colombiano.</summary>
    Cop,

    /// <summary>Real brasileiro.</summary>
    Brl,

    /// <summary>Peso argentino.</summary>
    Ars,

    /// <summary>Peso chileno.</summary>
    Clp,

    /// <summary>Colón costarriquenho.</summary>
    Crc,

    /// <summary>Peso dominicano.</summary>
    Dop,

    /// <summary>Quetzal guatemalteco.</summary>
    Gtq,

    /// <summary>Sol peruano.</summary>
    Pen,

    /// <summary>Guarani paraguaio.</summary>
    Pyg,

    /// <summary>Peso uruguaio.</summary>
    Uyu,

    /// <summary>USD Coin.</summary>
    Usdc,

    /// <summary>Tether.</summary>
    Usdt,
}

/// <summary>Estado de um payout.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<PayoutStatus>))]
public enum PayoutStatus
{
    /// <summary>Criado, aguardando processamento.</summary>
    Created,

    /// <summary>Enfileirado para o provedor de payout.</summary>
    Pending,

    /// <summary>Transferência em andamento.</summary>
    Processing,

    /// <summary>Entregue.</summary>
    Completed,

    /// <summary>Falhou.</summary>
    Failed,

    /// <summary>Fundos devolvidos pela instituição.</summary>
    Returned,
}

/// <summary>Tipo de documento de suporte anexado a um payout cripto.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<SupportingDocumentType>))]
public enum SupportingDocumentType
{
    /// <summary>Fatura.</summary>
    Invoice,

    /// <summary>Outro.</summary>
    Other,
}

/// <summary>Método de coleta de um PayIn.</summary>
[JsonConverter(typeof(UpperSnakeCaseEnumConverter<PayInMethod>))]
public enum PayInMethod
{
    /// <summary>Pagos Seguros en Línea (Colômbia). Uso único, exige <c>amount</c> e <c>bank_code</c>.</summary>
    Pse,

    /// <summary>SPEI (México). Reutilizável, não exige <c>amount</c> nem <c>bank_code</c>.</summary>
    Spei,
}

/// <summary>Tipo de identificador fiscal em <c>identifying_information</c>.</summary>
[JsonConverter(typeof(SnakeCaseLowerEnumConverter<IdentifyingInformationType>))]
public enum IdentifyingInformationType
{
    /// <summary>Social Security Number (EUA).</summary>
    Ssn,

    /// <summary>Employer Identification Number (EUA).</summary>
    Ein,

    /// <summary>CURP (México).</summary>
    Curp,

    /// <summary>RFC (México).</summary>
    Rfc,

    /// <summary>CPF (Brasil).</summary>
    Cpf,

    /// <summary>Taxpayer Identification Number.</summary>
    Tin,

    /// <summary>Identificador fiscal genérico.</summary>
    TaxId,
}
