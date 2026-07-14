namespace CambioReal.Contracts;

/// <summary>Gravidade de um <see cref="ProblemDetail"/> dentro de um <see cref="Envelope{T}"/>.</summary>
public enum ErrorSeverity
{
    /// <summary>Informativo — não impede o processamento nem exige ação do consumidor.</summary>
    Info,

    /// <summary>Aviso não bloqueante. Normalmente carregado em <see cref="Warning"/>, não em <see cref="ProblemDetail"/>.</summary>
    Warning,

    /// <summary>Erro que impediu a operação, mas é esperado no fluxo de negócio (validação, conflito, recurso ausente).</summary>
    Error,

    /// <summary>Falha inesperada — normalmente originada de exceção não tratada ou erro 5xx do provedor.</summary>
    Critical,
}
