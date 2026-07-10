using System.Text.Json;
using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>
/// Base de toda resposta da Kira.
/// </summary>
/// <remarks>
/// A Kira não publica OpenAPI e a documentação descreve os payloads em prosa. Campos que este SDK
/// ainda não modela caem em <see cref="AdditionalData"/> em vez de serem descartados — assim uma
/// resposta mais rica que o contrato conhecido não perde informação nem quebra a desserialização.
/// </remarks>
public abstract record KiraResponse
{
    /// <summary>Campos presentes na resposta que este SDK não modela.</summary>
    /// <remarks>
    /// Precisa de setter: o System.Text.Json não popula uma propriedade de extension data
    /// somente-leitura — ele a ignora em silêncio, sem erro.
    /// </remarks>
    [JsonExtensionData]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA2227:Collection properties should be read only",
        Justification = "Exigido pelo contrato de [JsonExtensionData] do System.Text.Json.")]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}

/// <summary>Página de uma coleção.</summary>
/// <typeparam name="T">Tipo do item.</typeparam>
public sealed record KiraPage<T> : KiraResponse
{
    /// <summary>Itens da página.</summary>
    public IReadOnlyList<T> Data { get; init; } = [];

    /// <summary>Total de itens, quando a Kira o informa.</summary>
    public int? Total { get; init; }

    /// <summary>Página atual.</summary>
    public int? Page { get; init; }

    /// <summary>Tamanho da página.</summary>
    public int? Limit { get; init; }
}

/// <summary>Paginação e ordenação comuns aos endpoints de listagem.</summary>
public sealed record KiraPageRequest
{
    /// <summary>Página, base 1.</summary>
    public int? Page { get; init; }

    /// <summary>Itens por página.</summary>
    public int? Limit { get; init; }
}

/// <summary>Sinalizações exigidas pelo produto ACT.</summary>
public sealed record AdditionalInfo
{
    /// <summary>Se o usuário possui conta bancária nos EUA.</summary>
    public bool? HasUsBankAccount { get; init; }

    /// <summary>Se o usuário já teve abertura de conta bancária recusada.</summary>
    public bool? HasDeniedBankAccount { get; init; }
}

/// <summary>
/// Arquivos anexados a um <see cref="IdentifyingInformation"/>, como data URI base64
/// (<c>data:application/pdf;base64,…</c>).
/// </summary>
public sealed record IdentifyingDocuments
{
    /// <summary>Frente do documento de identidade. Exigido salvo quando o documento é passaporte.</summary>
    public string? Front { get; init; }

    /// <summary>Verso do documento de identidade. Exigido salvo quando o documento é passaporte.</summary>
    public string? Back { get; init; }

    /// <summary>Comprovante de endereço.</summary>
    public string? FileProofOfAddress { get; init; }

    /// <summary>Comprovante de origem de recursos.</summary>
    public string? FileSourceOfWealth { get; init; }

    /// <summary>Ato constitutivo da empresa.</summary>
    public string? FileBusinessFormation { get; init; }
}

/// <summary>Identificador fiscal e seus documentos comprobatórios.</summary>
public sealed record IdentifyingInformation
{
    /// <summary>Natureza do identificador.</summary>
    public IdentifyingInformationType? Type { get; init; }

    /// <summary>Valor do identificador.</summary>
    public string? Number { get; init; }

    /// <summary>País emissor.</summary>
    public string? IssuingCountry { get; init; }

    /// <summary>Arquivos comprobatórios.</summary>
    public IdentifyingDocuments? Documents { get; init; }
}

/// <summary>Sócio, diretor ou representante de um usuário do tipo <see cref="UserType.Business"/>.</summary>
public sealed record AssociatedPerson
{
    /// <summary>Nome.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome.</summary>
    public string? LastName { get; init; }

    /// <summary>Data de nascimento.</summary>
    public DateOnly? BirthDate { get; init; }

    /// <summary>E-mail.</summary>
    public string? Email { get; init; }

    /// <summary>Nacionalidade (ISO 3166-1 alpha-3).</summary>
    public string? Nationality { get; init; }

    /// <summary>Número do documento de identidade.</summary>
    public string? DocumentNumber { get; init; }

    /// <summary>País emissor do documento.</summary>
    public string? DocumentCountry { get; init; }

    /// <summary>SSN. No produto ACT, exigido salvo quando a nacionalidade não é norte-americana.</summary>
    public string? Ssn { get; init; }

    /// <summary>Identificadores fiscais e documentos.</summary>
    public IReadOnlyList<IdentifyingInformation>? IdentifyingInformation { get; init; }
}
