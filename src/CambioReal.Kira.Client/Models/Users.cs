namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/users</c>.
/// </summary>
/// <remarks>
/// Quase todo campo é opcional no transporte porque a obrigatoriedade real da Kira é
/// <em>condicional</em>: ela depende do produto alvo (<see cref="KiraProduct"/>) e da categoria do
/// usuário, derivada de <see cref="AddressCountry"/> — <c>usa_individual</c>,
/// <c>international_individual</c>, <c>usa_business</c> ou <c>international_business</c>.
/// São oito conjuntos distintos de campos obrigatórios, e a Kira dispara a verificação assim que
/// <em>ao menos um</em> produto tiver o seu conjunto completo. Codificar isso em tipos exigiria
/// oito requests distintos; em vez disso, a resposta traz <see cref="KiraUser.MissingFields"/>
/// e <see cref="KiraUser.EligibleProducts"/> para você reagir.
/// <para>
/// Com <see cref="VerificationMode.VerificationLink"/> bastam <see cref="FirstName"/>,
/// <see cref="LastName"/> e <see cref="Email"/> (ou <see cref="BusinessLegalName"/> e
/// <see cref="Email"/>, para empresas).
/// </para>
/// </remarks>
public sealed record CreateUserRequest
{
    /// <summary>Pessoa física ou jurídica.</summary>
    public required UserType Type { get; init; }

    /// <summary>Como coletar o KYC/KYB. O padrão da Kira é <see cref="VerificationMode.Automatic"/>.</summary>
    public VerificationMode? VerificationMode { get; init; }

    /// <summary>Para onde redirecionar o usuário após o fluxo hospedado de verificação.</summary>
    public Uri? RedirectUri { get; init; }

    /// <summary>Seu identificador interno, ecoado nos webhooks.</summary>
    public string? ExtReference { get; init; }

    /// <summary>E-mail. Obrigatório em todas as categorias.</summary>
    public string? Email { get; init; }

    /// <summary>Telefone.</summary>
    public string? Phone { get; init; }

    /// <summary>IP de origem do cadastro.</summary>
    public string? IpAddress { get; init; }

    /// <summary>Nome.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome.</summary>
    public string? LastName { get; init; }

    /// <summary>Data de nascimento.</summary>
    public DateOnly? BirthDate { get; init; }

    /// <summary>Nacionalidade (ISO 3166-1 alpha-3).</summary>
    public string? Nationality { get; init; }

    /// <summary>Logradouro.</summary>
    public string? AddressStreet { get; init; }

    /// <summary>Cidade.</summary>
    public string? AddressCity { get; init; }

    /// <summary>Estado. Não exigido para empresas no produto Diameter.</summary>
    public string? AddressState { get; init; }

    /// <summary>CEP.</summary>
    public string? AddressZipCode { get; init; }

    /// <summary>
    /// País do endereço (ISO 3166-1 alpha-3). Determina a categoria de campos obrigatórios e
    /// o bloqueio por país restrito — ver <see cref="KiraRestrictedCountries"/>.
    /// </summary>
    public string? AddressCountry { get; init; }

    /// <summary>Tipo do documento de identidade. <c>passport</c> dispensa o verso.</summary>
    public string? DocumentType { get; init; }

    /// <summary>Número do documento.</summary>
    public string? DocumentNumber { get; init; }

    /// <summary>País emissor do documento.</summary>
    public string? DocumentCountry { get; init; }

    /// <summary>SSN. Exigido para pessoa física nos EUA.</summary>
    public string? Ssn { get; init; }

    /// <summary>EIN. Exigido para empresa nos EUA.</summary>
    public string? Ein { get; init; }

    /// <summary>Razão social.</summary>
    public string? BusinessLegalName { get; init; }

    /// <summary>Nome fantasia.</summary>
    public string? DoingBusinessAs { get; init; }

    /// <summary>Natureza jurídica.</summary>
    public string? BusinessType { get; init; }

    /// <summary>Setor de atuação.</summary>
    public string? BusinessIndustry { get; init; }

    /// <summary>Data de constituição.</summary>
    public DateOnly? FormationDate { get; init; }

    /// <summary>País de constituição.</summary>
    public string? FormationCountry { get; init; }

    /// <summary>Natureza da entidade estrangeira. Exigido para empresa internacional no produto ACT.</summary>
    public string? InternationalEntityType { get; init; }

    /// <summary>Situação empregatícia. Exigido no produto ACT.</summary>
    public string? EmploymentStatus { get; init; }

    /// <summary>Empregador atual. Exigido quando <see cref="EmploymentStatus"/> é <c>employed</c>.</summary>
    public string? CurrentEmployer { get; init; }

    /// <summary>Situação imigratória. Exigido para pessoa física internacional no produto ACT.</summary>
    public string? ImmigrationStatus { get; init; }

    /// <summary>Sinalizações bancárias exigidas pelo produto ACT.</summary>
    public AdditionalInfo? AdditionalInfo { get; init; }

    /// <summary>Identificadores fiscais e documentos.</summary>
    public IReadOnlyList<IdentifyingInformation>? IdentifyingInformation { get; init; }

    /// <summary>Sócios e representantes. Exigido para empresas.</summary>
    public IReadOnlyList<AssociatedPerson>? AssociatedPersons { get; init; }
}

/// <summary>
/// Corpo de <c>PATCH /v1/users/{id}</c>. Todos os campos são opcionais; só o que você enviar muda.
/// Alterar um campo sensível (nome, nascimento, endereço, SSN, documento) dispara reverificação.
/// </summary>
public sealed record UpdateUserRequest
{
    /// <summary>E-mail.</summary>
    public string? Email { get; init; }

    /// <summary>Telefone.</summary>
    public string? Phone { get; init; }

    /// <summary>Nome.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome.</summary>
    public string? LastName { get; init; }

    /// <summary>Data de nascimento. Sensível: dispara reverificação.</summary>
    public DateOnly? BirthDate { get; init; }

    /// <summary>Nacionalidade.</summary>
    public string? Nationality { get; init; }

    /// <summary>Logradouro. Sensível.</summary>
    public string? AddressStreet { get; init; }

    /// <summary>Cidade.</summary>
    public string? AddressCity { get; init; }

    /// <summary>Estado.</summary>
    public string? AddressState { get; init; }

    /// <summary>CEP.</summary>
    public string? AddressZipCode { get; init; }

    /// <summary>País do endereço.</summary>
    public string? AddressCountry { get; init; }

    /// <summary>Tipo do documento. Sensível.</summary>
    public string? DocumentType { get; init; }

    /// <summary>Número do documento. Sensível.</summary>
    public string? DocumentNumber { get; init; }

    /// <summary>País emissor.</summary>
    public string? DocumentCountry { get; init; }

    /// <summary>SSN. Sensível.</summary>
    public string? Ssn { get; init; }

    /// <summary>EIN. Sensível.</summary>
    public string? Ein { get; init; }

    /// <summary>Razão social.</summary>
    public string? BusinessLegalName { get; init; }

    /// <summary>Nome fantasia.</summary>
    public string? DoingBusinessAs { get; init; }

    /// <summary>Situação empregatícia.</summary>
    public string? EmploymentStatus { get; init; }

    /// <summary>Empregador atual.</summary>
    public string? CurrentEmployer { get; init; }

    /// <summary>Situação imigratória.</summary>
    public string? ImmigrationStatus { get; init; }

    /// <summary>Sinalizações bancárias.</summary>
    public AdditionalInfo? AdditionalInfo { get; init; }

    /// <summary>Identificadores fiscais e documentos.</summary>
    public IReadOnlyList<IdentifyingInformation>? IdentifyingInformation { get; init; }

    /// <summary>Sócios e representantes.</summary>
    public IReadOnlyList<AssociatedPerson>? AssociatedPersons { get; init; }
}

/// <summary>Usuário devolvido pela Kira.</summary>
public record KiraUser : KiraResponse
{
    /// <summary>Identificador do usuário.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Seu identificador interno.</summary>
    public string? ExtReference { get; init; }

    /// <summary>Pessoa física ou jurídica.</summary>
    public UserType? Type { get; init; }

    /// <summary>Situação de verificação.</summary>
    public VerificationStatus? VerificationStatus { get; init; }

    /// <summary>Profundidade do KYC concluído.</summary>
    public KycType? KycType { get; init; }

    /// <summary>E-mail.</summary>
    public string? Email { get; init; }

    /// <summary>Telefone.</summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Produtos para os quais o usuário já reúne todos os campos exigidos.
    /// Elegibilidade não é um booleano: é um conjunto, por produto.
    /// </summary>
    public IReadOnlyList<KiraProduct> EligibleProducts { get; init; } = [];

    /// <summary>Campos de KYC/KYB ainda faltantes.</summary>
    public IReadOnlyList<string> MissingFields { get; init; } = [];

    /// <summary>Criação.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>Resposta de <c>POST /v1/users</c>.</summary>
public sealed record CreateUserResponse : KiraUser
{
    /// <summary>Se a verificação foi de fato disparada — ou seja, se algum produto ficou completo.</summary>
    public bool? VerificationTriggered { get; init; }

    /// <summary>Link hospedado de verificação, quando <see cref="VerificationMode.VerificationLink"/> foi usado.</summary>
    public Uri? VerificationLink { get; init; }
}

/// <summary>Resposta de <c>PATCH /v1/users/{id}</c>.</summary>
public sealed record UpdateUserResponse : KiraUser
{
    /// <summary>Campos que de fato mudaram.</summary>
    public IReadOnlyList<string> UpdatedFields { get; init; } = [];

    /// <summary>Se a alteração disparou reverificação de KYC/KYB.</summary>
    public bool? RequiresReverification { get; init; }

    /// <summary>Problemas não fatais, como falha no upload de um documento.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>Filtros de <c>GET /v1/users</c>.</summary>
public sealed record ListUsersRequest
{
    /// <summary>Situação do usuário.</summary>
    public string? Status { get; init; }

    /// <summary>Pessoa física ou jurídica.</summary>
    public UserType? Type { get; init; }

    /// <summary>Situação de verificação.</summary>
    public VerificationStatus? VerificationStatus { get; init; }

    /// <summary>E-mail exato.</summary>
    public string? Email { get; init; }

    /// <summary>Criados a partir de.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Criados até.</summary>
    public DateTimeOffset? CreatedTo { get; init; }

    /// <summary>Página, base 1.</summary>
    public int? Page { get; init; }

    /// <summary>Itens por página.</summary>
    public int? Limit { get; init; }
}

/// <summary>
/// Corpo de <c>POST /v1/users/{id}/verifications</c> — fluxo manual, legado.
/// O caminho recomendado é enviar os campos completos na criação do usuário.
/// </summary>
public sealed record CreateVerificationRequest
{
    /// <summary><c>embedded-link</c> para o fluxo hospedado, <c>api</c> para o programático.</summary>
    public required string Method { get; init; }

    /// <summary>Destino após a conclusão, no fluxo hospedado.</summary>
    public Uri? RedirectUri { get; init; }
}

/// <summary>Resposta de <c>POST /v1/users/{id}/verifications</c>.</summary>
public sealed record CreateVerificationResponse : KiraResponse
{
    /// <summary>Identificador da verificação.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Link hospedado, no método <c>embedded-link</c>.</summary>
    public Uri? VerificationLink { get; init; }

    /// <summary>Situação.</summary>
    public VerificationStatus? Status { get; init; }
}

/// <summary>
/// Países bloqueados para os produtos de conta virtual (ISO 3166-1 alpha-3).
/// </summary>
/// <remarks>
/// Verificar localmente antes de chamar a Kira evita um round-trip e uma rejeição de KYC.
/// A lista vem da documentação de <c>createUser</c> e pode mudar sem aviso — trate-a como um
/// atalho, não como a autoridade.
/// </remarks>
public static class KiraRestrictedCountries
{
    private static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AFG", "BLR", "MMR", "CAF", "CIV", "CUB", "COD", "IRN", "IRQ", "LBR", "LBY",
        "PRK", "RUS", "SAU", "SOM", "SDN", "SYR", "UKR", "YEM", "VEN", "ZWE",
    };

    /// <summary>Se o país está bloqueado para produtos de conta virtual.</summary>
    public static bool IsRestricted(string? alpha3CountryCode) =>
        !string.IsNullOrWhiteSpace(alpha3CountryCode) && Codes.Contains(alpha3CountryCode);
}
