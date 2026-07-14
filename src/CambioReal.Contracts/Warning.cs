namespace CambioReal.Contracts;

/// <summary>
/// Aviso não bloqueante dentro de <see cref="Envelope{T}.Warnings"/> — nunca impede o
/// processamento da operação (ex.: limite próximo, documento prestes a expirar, cotação perto do
/// vencimento, configuração obsoleta).
/// </summary>
public sealed record Warning
{
    /// <summary>Código funcional estável (ex.: <c>QUOTE_NEAR_EXPIRY</c>).</summary>
    public required string Code { get; init; }

    /// <summary>Mensagem amigável.</summary>
    public required string Message { get; init; }

    /// <summary>Campo ao qual este aviso se refere, quando aplicável.</summary>
    public string? Field { get; init; }

    /// <summary>URL de documentação para este aviso.</summary>
    public Uri? DocumentationUrl { get; init; }
}
