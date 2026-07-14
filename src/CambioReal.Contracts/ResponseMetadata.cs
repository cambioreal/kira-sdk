namespace CambioReal.Contracts;

/// <summary>
/// Metadados técnicos de um <see cref="Envelope{T}"/>.
/// </summary>
/// <remarks>
/// Em um consumidor que seja uma biblioteca cliente (ex.: o kira-sdk), nem todo campo é
/// conhecido no ponto de origem — <see cref="TenantId"/>, <see cref="AuthenticatedUser"/> e
/// <see cref="Locale"/>, por exemplo, pertencem ao contexto HTTP de quem consome o SDK (ex.: um
/// controller ASP.NET Core em cambio-real-v3), não ao SDK em si. Populá-los é responsabilidade
/// do chamador ao montar seu próprio <see cref="Envelope{T}"/> de saída — o SDK só preenche o
/// que legitimamente sabe (<see cref="Service"/>, <see cref="ApiVersion"/>,
/// <see cref="Timestamp"/>, <see cref="DurationMs"/> quando medido).
/// </remarks>
public sealed record ResponseMetadata
{
    /// <summary>Instante da resposta, em UTC.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Identificador único desta requisição.</summary>
    public string? RequestId { get; init; }

    /// <summary>Identificador de correlação, propagado através de múltiplos serviços/mensagens.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Identificador de trace distribuído (OpenTelemetry/W3C Trace Context).</summary>
    public string? TraceId { get; init; }

    /// <summary>Identificador do span atual dentro do trace.</summary>
    public string? SpanId { get; init; }

    /// <summary>Duração do processamento, em milissegundos.</summary>
    public long? DurationMs { get; init; }

    /// <summary>Versão da API/contrato que produziu esta resposta.</summary>
    public string? ApiVersion { get; init; }

    /// <summary>Serviço que produziu esta resposta.</summary>
    public string? Service { get; init; }

    /// <summary>Ambiente de execução (ex.: <c>production</c>, <c>sandbox</c>).</summary>
    public string? Environment { get; init; }

    /// <summary>Região/datacenter de origem.</summary>
    public string? Region { get; init; }

    /// <summary>Tenant associado à requisição, quando a plataforma for multi-tenant.</summary>
    public string? TenantId { get; init; }

    /// <summary>Identificador do usuário/principal autenticado que originou a requisição.</summary>
    public string? AuthenticatedUser { get; init; }

    /// <summary>Locale usado para localizar <c>message</c>/<c>title</c>/<c>detail</c>.</summary>
    public string? Locale { get; init; }

    /// <summary>Metadados de paginação, quando <see cref="Envelope{T}.Data"/> for uma coleção paginada.</summary>
    public PagedMetadata? Pagination { get; init; }

    /// <summary>
    /// Campos técnicos adicionais, aplicáveis apenas em alguns contextos: cache, rate limit,
    /// idempotency key, retry count, processing node, deployment version, feature flags,
    /// execution mode. Um dicionário aberto em vez de propriedades fixas — nem todo contexto se
    /// conhece de antemão, e não deve ser descartado.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; init; }
}
