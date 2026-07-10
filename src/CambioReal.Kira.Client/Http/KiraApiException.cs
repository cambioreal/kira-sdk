using System.Net;

namespace CambioReal.Kira.Http;

/// <summary>Erro devolvido pela API Kira.</summary>
public class KiraApiException : Exception
{
    /// <summary>Cria uma exceção sem contexto de resposta.</summary>
    public KiraApiException()
    {
    }

    /// <summary>Cria uma exceção com mensagem.</summary>
    public KiraApiException(string message)
        : base(message)
    {
    }

    /// <summary>Cria uma exceção com mensagem e causa.</summary>
    public KiraApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Cria uma exceção a partir de uma resposta da API.</summary>
    public KiraApiException(HttpStatusCode statusCode, string? errorCode, string message, string? responseBody)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        ResponseBody = responseBody;
    }

    /// <summary>Status HTTP da resposta.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Código de erro da Kira, quando presente (ex.: <c>VALIDATION_REQUIRED</c>, <c>OTP_INVALID</c>).</summary>
    public string? ErrorCode { get; }

    /// <summary>Corpo bruto da resposta, para diagnóstico.</summary>
    public string? ResponseBody { get; }
}

/// <summary>A autenticação falhou mesmo após uma renovação de token.</summary>
public sealed class KiraAuthenticationException : KiraApiException
{
    /// <inheritdoc/>
    public KiraAuthenticationException()
    {
    }

    /// <inheritdoc/>
    public KiraAuthenticationException(string message)
        : base(message)
    {
    }

    /// <inheritdoc/>
    public KiraAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public KiraAuthenticationException(HttpStatusCode statusCode, string? errorCode, string message, string? responseBody)
        : base(statusCode, errorCode, message, responseBody)
    {
    }
}

/// <summary>
/// A chave de idempotência já foi usada com um corpo de requisição diferente (HTTP 409).
/// Reenviar não resolve: ou reutilize o corpo original, ou gere uma chave nova.
/// </summary>
public sealed class KiraIdempotencyConflictException : KiraApiException
{
    /// <inheritdoc/>
    public KiraIdempotencyConflictException()
    {
    }

    /// <inheritdoc/>
    public KiraIdempotencyConflictException(string message)
        : base(message)
    {
    }

    /// <inheritdoc/>
    public KiraIdempotencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <inheritdoc/>
    public KiraIdempotencyConflictException(HttpStatusCode statusCode, string? errorCode, string message, string? responseBody)
        : base(statusCode, errorCode, message, responseBody)
    {
    }
}
