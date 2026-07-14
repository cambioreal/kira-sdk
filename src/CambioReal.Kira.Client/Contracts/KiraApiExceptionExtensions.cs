using System.Net;
using System.Text.Json;
using CambioReal.Contracts;
using CambioReal.Kira.Http;

namespace CambioReal.Kira.Contracts;

/// <summary>
/// Traduz <see cref="KiraApiException"/> para o contrato canônico (<see cref="ProblemDetail"/>,
/// de <c>CambioReal.Contracts</c>) — a demonstração viva de como este SDK se encaixa no
/// <see cref="Envelope{T}"/> de quem o consome.
/// </summary>
public static class KiraApiExceptionExtensions
{
    private const string DocumentationBaseUrl = "https://errors.cambioreal.dev/";

    /// <summary>
    /// Traduz a exceção em um ou mais <see cref="ProblemDetail"/> — um por campo, quando a Kira
    /// devolveu a forma <c>{"error": "...", "details": [{"path","message","code"}, ...]}</c>
    /// (confirmada contra o sandbox em 2026-07-13, ver README "Confirmado contra o sandbox").
    /// Quando a resposta não tem esse formato — a maioria dos casos, já que a Kira usa pelo menos
    /// 6 formatos de erro distintos por endpoint — devolve uma lista com um único
    /// <see cref="ProblemDetail"/> a partir de <see cref="KiraApiException.ErrorCode"/> e
    /// <see cref="Exception.Message"/>.
    /// </summary>
    public static IReadOnlyList<ProblemDetail> ToProblemDetails(this KiraApiException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var fieldErrors = TryExtractFieldErrors(exception);

        return fieldErrors ?? [ToSingleProblemDetail(exception)];
    }

    /// <summary>Traduz a exceção em um único <see cref="ProblemDetail"/>, ignorando <c>details[]</c> por campo.</summary>
    public static ProblemDetail ToSingleProblemDetail(this KiraApiException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var code = exception.ErrorCode ?? DefaultCodeFor(exception);

        return new ProblemDetail
        {
            Type = DocumentationUrlFor(code),
            Status = (int)exception.StatusCode,
            Code = code,
            Title = DefaultTitleFor(exception),
            Detail = exception.Message,
            Retryable = IsRetryable(exception.StatusCode),
            Severity = SeverityFor(exception.StatusCode),
        };
    }

    private static List<ProblemDetail>? TryExtractFieldErrors(KiraApiException exception)
    {
        if (string.IsNullOrWhiteSpace(exception.ResponseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(exception.ResponseBody);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("details", out var details)
                || details.ValueKind != JsonValueKind.Array
                || details.GetArrayLength() == 0)
            {
                return null;
            }

            var title = root.TryGetProperty("error", out var errorValue) && errorValue.ValueKind == JsonValueKind.String
                ? errorValue.GetString()!
                : DefaultTitleFor(exception);

            var problems = new List<ProblemDetail>();

            foreach (var item in details.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var field = GetStringOrNull(item, "path") ?? GetStringOrNull(item, "field");
                var detail = GetStringOrNull(item, "message");
                var itemCode = GetStringOrNull(item, "code") ?? exception.ErrorCode ?? DefaultCodeFor(exception);

                if (detail is null)
                {
                    continue;
                }

                problems.Add(new ProblemDetail
                {
                    Type = DocumentationUrlFor(itemCode),
                    Status = (int)exception.StatusCode,
                    Code = itemCode,
                    Title = title,
                    Detail = detail,
                    Field = field,
                    Retryable = IsRetryable(exception.StatusCode),
                    Severity = SeverityFor(exception.StatusCode),
                });
            }

            return problems.Count > 0 ? problems : null;
        }
        catch (JsonException)
        {
            // Corpo não é o formato com details[] estruturado — cai para o ProblemDetail único.
            return null;
        }
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string DefaultCodeFor(KiraApiException exception) => exception switch
    {
        KiraAuthenticationException => "AUTHENTICATION_FAILED",
        KiraIdempotencyConflictException => "IDEMPOTENCY_CONFLICT",
        _ => "KIRA_API_ERROR",
    };

    private static string DefaultTitleFor(KiraApiException exception) => exception switch
    {
        KiraAuthenticationException => "Falha de autenticação",
        KiraIdempotencyConflictException => "Conflito de idempotência",
        _ => "Erro na API Kira",
    };

    /// <summary>
    /// <see langword="true"/> quando repetir a mesma chamada, sem alterações, tem chance
    /// razoável de suceder.
    /// </summary>
    private static bool IsRetryable(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.TooManyRequests => true,
        HttpStatusCode.BadGateway => true,
        HttpStatusCode.ServiceUnavailable => true,
        HttpStatusCode.GatewayTimeout => true,
        HttpStatusCode.InternalServerError => true,
        _ => false,
    };

    private static ErrorSeverity SeverityFor(HttpStatusCode statusCode) =>
        (int)statusCode >= 500 ? ErrorSeverity.Critical : ErrorSeverity.Error;

    private static Uri DocumentationUrlFor(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? new Uri("about:blank")
            : new Uri(DocumentationBaseUrl + code.Replace('_', '-').ToLowerInvariant());
}
