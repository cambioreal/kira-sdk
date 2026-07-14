namespace CambioReal.Kira.Contracts;

/// <summary>
/// Metadados de paginação de uma coleção, isolados de <see cref="Envelope{T}.Data"/> — a
/// paginação nunca se mistura ao payload.
/// </summary>
public sealed record PagedMetadata
{
    /// <summary>Página atual, base 1.</summary>
    public required int Page { get; init; }

    /// <summary>Itens por página.</summary>
    public required int PageSize { get; init; }

    /// <summary>Total de itens na coleção, quando conhecido.</summary>
    public int? TotalItems { get; init; }

    /// <summary>Total de páginas, quando conhecido.</summary>
    public int? TotalPages { get; init; }

    /// <summary>Se há próxima página.</summary>
    public required bool HasNext { get; init; }

    /// <summary>Se há página anterior.</summary>
    public required bool HasPrevious { get; init; }
}
