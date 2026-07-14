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
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-13 (<c>GET /v1/users</c>, <c>GET /v1/virtual-accounts</c>):
/// a resposta é <c>{"data": [...], "pagination": {total, limit, offset, has_more}}</c> — não os
/// campos <c>total</c>/<c>page</c>/<c>limit</c> soltos na raiz que esta versão assumia antes.
/// </remarks>
public sealed record KiraPage<T> : KiraResponse
{
    /// <summary>Itens da página.</summary>
    public IReadOnlyList<T> Data { get; init; } = [];

    /// <summary>Metadados de paginação, quando a Kira os informa.</summary>
    public KiraPagination? Pagination { get; init; }
}

/// <summary>Metadados de paginação de uma <see cref="KiraPage{T}"/>.</summary>
/// <remarks>
/// Paginação por <b>offset</b>, não por número de página — apesar do parâmetro de query se
/// chamar <c>page</c> nos filtros de listagem (<see cref="KiraPageRequest"/> e afins). Não
/// confirmado ainda se o parâmetro de request <c>page</c> é aceito como está ou se a Kira
/// espera <c>offset</c> — só o formato da resposta foi sondado contra o sandbox.
/// </remarks>
public sealed record KiraPagination : KiraResponse
{
    /// <summary>Total de itens na coleção.</summary>
    public int? Total { get; init; }

    /// <summary>Tamanho da página.</summary>
    public int? Limit { get; init; }

    /// <summary>Deslocamento do primeiro item desta página.</summary>
    public int? Offset { get; init; }

    /// <summary>Se há mais itens além desta página.</summary>
    public bool? HasMore { get; init; }
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
