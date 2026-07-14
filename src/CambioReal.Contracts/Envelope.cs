namespace CambioReal.Contracts;

/// <summary>
/// Contrato canônico de resposta da plataforma — o mesmo formato para sucesso e erro,
/// independente de módulo, domínio ou serviço. Ver
/// <c>_pipeline/rfc-kira-sdk-canonical-response-envelope.md</c> no vault para o RFC completo.
/// </summary>
/// <remarks>
/// <para>
/// Este pacote (<c>CambioReal.Contracts</c>) não depende de nenhuma integração ou serviço
/// específico — é a peça reutilizável entre repositórios. O <c>kira-sdk</c> (client library) o
/// referencia sem inverter a dependência: <c>KiraClient</c> continua devolvendo <c>T</c>
/// diretamente e lançando <c>KiraApiException</c> em falha (idioma de client library — RFC 9457
/// no §2 do RFC completo explica o porquê); <c>KiraApiExceptionExtensions.ToProblemDetails()</c>,
/// do lado do kira-sdk, traduz essas exceções para <see cref="ProblemDetail"/>, prontos para
/// compor um <see cref="Envelope{T}"/> de saída — tipicamente via <c>Envelope.Ok(data, ...)</c>
/// no caminho feliz e <c>Envelope.Fail&lt;T&gt;(problemDetails, ...)</c> na falha, dentro de um
/// serviço HTTP real (ex.: um controller ASP.NET Core em cambio-real-v3).
/// </para>
/// <para>
/// <see cref="Data"/> é <see langword="null"/> quando não há conteúdo — nunca <c>{}</c>, <c>[]</c>
/// ou <c>""</c> para representar ausência.
/// </para>
/// </remarks>
/// <typeparam name="T">Tipo do payload da operação.</typeparam>
public sealed record Envelope<T>
{
    /// <summary>
    /// Indica sucesso da operação. Nunca inferido a partir do status HTTP — é o campo
    /// autoritativo.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Código funcional estável (ex.: <c>CUSTOMER_CREATED</c>, <c>VALIDATION_ERROR</c>). Único,
    /// imutável, documentado — nunca uma mensagem.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>Mensagem amigável, voltada ao consumidor. Nunca usada para tomada de decisão.</summary>
    public required string Message { get; init; }

    /// <summary>Payload da operação. <see langword="null"/> quando não há conteúdo.</summary>
    public T? Data { get; init; }

    /// <summary>Erros desta resposta. Vazio quando <see cref="Success"/> é <see langword="true"/>.</summary>
    public IReadOnlyList<ProblemDetail> Errors { get; init; } = [];

    /// <summary>Avisos não bloqueantes.</summary>
    public IReadOnlyList<Warning> Warnings { get; init; } = [];

    /// <summary>Metadados técnicos desta resposta.</summary>
    public required ResponseMetadata Metadata { get; init; }

    /// <summary>Links HATEOAS opcionais (ex.: <c>self</c>, <c>next</c>, <c>previous</c>, <c>documentation</c>, <c>related</c>).</summary>
    public IReadOnlyDictionary<string, Uri>? Links { get; init; }
}

/// <summary>
/// Fábrica de <see cref="Envelope{T}"/>. Um tipo não-genérico separado (em vez de membros
/// estáticos em <see cref="Envelope{T}"/>) para permitir inferência do argumento de tipo na
/// chamada — <c>Envelope.Ok(data, ...)</c>, não <c>Envelope&lt;Foo&gt;.Ok(...)</c>.
/// </summary>
public static class Envelope
{
    /// <summary>Constrói uma resposta de sucesso.</summary>
    /// <param name="data">Payload. <see langword="null"/> quando não há conteúdo a devolver.</param>
    /// <param name="code">Código funcional estável (ex.: <c>CUSTOMER_CREATED</c>).</param>
    /// <param name="message">Mensagem amigável.</param>
    /// <param name="metadata">Metadados técnicos. Quando omitido, usa um mínimo com <see cref="ResponseMetadata.Timestamp"/> apenas.</param>
    /// <param name="warnings">Avisos não bloqueantes, se houver.</param>
    /// <param name="links">Links HATEOAS, se houver.</param>
    public static Envelope<T> Ok<T>(
        T? data,
        string code,
        string message,
        ResponseMetadata? metadata = null,
        IReadOnlyList<Warning>? warnings = null,
        IReadOnlyDictionary<string, Uri>? links = null) =>
        new()
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data,
            Warnings = warnings ?? [],
            Metadata = metadata ?? new ResponseMetadata { Timestamp = DateTimeOffset.UtcNow },
            Links = links,
        };

    /// <summary>Constrói uma resposta de falha.</summary>
    /// <param name="errors">Erros desta resposta. Ao menos um elemento, mesmo que a falha seja única.</param>
    /// <param name="code">Código funcional estável (ex.: <c>VALIDATION_ERROR</c>).</param>
    /// <param name="message">Mensagem amigável.</param>
    /// <param name="metadata">Metadados técnicos. Quando omitido, usa um mínimo com <see cref="ResponseMetadata.Timestamp"/> apenas.</param>
    /// <param name="warnings">Avisos não bloqueantes, se houver.</param>
    public static Envelope<T> Fail<T>(
        IReadOnlyList<ProblemDetail> errors,
        string code,
        string message,
        ResponseMetadata? metadata = null,
        IReadOnlyList<Warning>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            throw new ArgumentException("Uma resposta de falha precisa de ao menos um erro.", nameof(errors));
        }

        return new Envelope<T>
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default,
            Errors = errors,
            Warnings = warnings ?? [],
            Metadata = metadata ?? new ResponseMetadata { Timestamp = DateTimeOffset.UtcNow },
        };
    }
}
