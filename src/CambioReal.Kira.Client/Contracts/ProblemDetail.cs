namespace CambioReal.Kira.Contracts;

/// <summary>
/// Um erro dentro de <see cref="Envelope{T}.Errors"/>, seguindo RFC 9457 (Problem Details for
/// HTTP APIs) com os campos adicionais exigidos pelo contrato canônico da plataforma.
/// </summary>
/// <remarks>
/// Ver <c>docs/SPEC.md</c> deste RFC (<c>_pipeline/rfc-kira-sdk-canonical-response-envelope.md</c>
/// no vault) para o racional completo. <see cref="Code"/> é o identificador estável e imutável
/// (ex.: <c>VALIDATION_ERROR</c>) — nunca a mensagem; <see cref="Title"/>/<see cref="Detail"/> são
/// texto amigável, nunca usados para tomada de decisão programática.
/// </remarks>
public sealed record ProblemDetail
{
    /// <summary>
    /// URI que identifica o tipo do problema (RFC 9457 §3.1). Convenção desta plataforma:
    /// <c>https://errors.cambioreal.dev/{code-kebab-case}</c>. Use <c>about:blank</c> quando não
    /// houver uma URI mais específica.
    /// </summary>
    public required Uri Type { get; init; }

    /// <summary>Código de status HTTP associado a este problema (RFC 9457 §3.1.2).</summary>
    public required int Status { get; init; }

    /// <summary>
    /// Código funcional estável e imutável (ex.: <c>VALIDATION_ERROR</c>, <c>INSUFFICIENT_BALANCE</c>).
    /// Documentado, único, consumido por frontend/integrações/automações. Nunca uma mensagem.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>Resumo curto e legível do tipo de problema (RFC 9457 §3.1.3). Pode ser localizado.</summary>
    public required string Title { get; init; }

    /// <summary>Explicação específica desta ocorrência (RFC 9457 §3.1.4). Pode ser localizado.</summary>
    public string? Detail { get; init; }

    /// <summary>Campo do payload de entrada ao qual este erro se refere, quando aplicável (validação).</summary>
    public string? Field { get; init; }

    /// <summary>
    /// Identificador do recurso/entidade ao qual este erro se refere, quando aplicável — RFC 9457
    /// <c>instance</c> generalizado para não-HTTP (ex.: um <c>recipientId</c>).
    /// </summary>
    public string? Target { get; init; }

    /// <summary>URL de documentação para este código de erro específico.</summary>
    public Uri? DocumentationUrl { get; init; }

    /// <summary>
    /// Se repetir a mesma operação sem alterações tem chance razoável de suceder (ex.: <c>true</c>
    /// para <c>429</c>/<c>502</c>/<c>503</c>/<c>504</c>; <c>false</c> para erro de validação ou
    /// credencial inválida).
    /// </summary>
    public required bool Retryable { get; init; }

    /// <summary>Gravidade do erro.</summary>
    public required ErrorSeverity Severity { get; init; }
}
