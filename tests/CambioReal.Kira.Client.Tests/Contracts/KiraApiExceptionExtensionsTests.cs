using System.Net;
using CambioReal.Contracts;
using CambioReal.Kira.Contracts;
using CambioReal.Kira.Http;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests.Contracts;

public sealed class KiraApiExceptionExtensionsTests
{
    [Fact]
    public void SingleErrorBodyProducesOneProblemDetail()
    {
        var exception = new KiraApiException(
            HttpStatusCode.NotFound,
            "not_found",
            "A Kira respondeu HTTP 404 (NotFound): not_found.",
            """{"code":"not_found","message":"User with ID x not found"}""");

        var problems = exception.ToProblemDetails();

        problems.Count.ShouldBe(1);
        problems[0].Code.ShouldBe("not_found");
        problems[0].Status.ShouldBe(404);
        problems[0].Retryable.ShouldBeFalse();
        problems[0].Severity.ShouldBe(ErrorSeverity.Error);
    }

    /// <summary>
    /// Formato confirmado contra o sandbox em 2026-07-13
    /// (<c>{"error":"...","details":[{"path","message","code"}, ...]}</c>): um
    /// <see cref="ProblemDetail"/> por campo, não uma mensagem concatenada.
    /// </summary>
    [Fact]
    public void FieldValidationBodyProducesOneProblemDetailPerField()
    {
        var body = """
            {"error":"Invalid request data","details":[
                {"path":"user_id","message":"Required","code":"invalid_type"},
                {"path":"amount","message":"Expected string, received number","code":"invalid_type"},
                {"path":"currency","message":"Required","code":"invalid_type"}
            ]}
            """;

        var exception = new KiraApiException(
            HttpStatusCode.BadRequest, errorCode: null, "A Kira respondeu HTTP 400 (BadRequest).", body);

        var problems = exception.ToProblemDetails();

        problems.Count.ShouldBe(3);
        problems.ShouldAllBe(p => p.Title == "Invalid request data");
        problems[0].Field.ShouldBe("user_id");
        problems[0].Detail.ShouldBe("Required");
        problems[1].Field.ShouldBe("amount");
        problems[1].Detail.ShouldBe("Expected string, received number");
    }

    [Fact]
    public void NestedErrorObjectFallsBackToASingleProblemDetail()
    {
        var body = """{"error":{"code":"USER_NOT_FOUND","message":"User with ID x not found","details":{}}}""";

        var exception = new KiraApiException(HttpStatusCode.NotFound, "USER_NOT_FOUND", "erro", body);

        var problems = exception.ToProblemDetails();

        // details aqui é um objeto (contexto), não um array por campo — sem extração multi-erro.
        problems.Count.ShouldBe(1);
        problems[0].Code.ShouldBe("USER_NOT_FOUND");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Conflict, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public void RetryableIsComputedFromStatusCode(HttpStatusCode statusCode, bool expectedRetryable)
    {
        var exception = new KiraApiException(statusCode, "X", "erro", responseBody: null);

        exception.ToSingleProblemDetail().Retryable.ShouldBe(expectedRetryable);
    }

    [Fact]
    public void ServerErrorsAreCriticalClientErrorsAreNot()
    {
        new KiraApiException(HttpStatusCode.InternalServerError, "X", "erro", null)
            .ToSingleProblemDetail().Severity.ShouldBe(ErrorSeverity.Critical);

        new KiraApiException(HttpStatusCode.BadRequest, "X", "erro", null)
            .ToSingleProblemDetail().Severity.ShouldBe(ErrorSeverity.Error);
    }

    [Fact]
    public void AuthenticationExceptionGetsItsOwnDefaultCodeWhenTheBodyHasNone()
    {
        var exception = new KiraAuthenticationException(HttpStatusCode.Unauthorized, errorCode: null, "falha", responseBody: null);

        var problem = exception.ToSingleProblemDetail();

        problem.Code.ShouldBe("AUTHENTICATION_FAILED");
        problem.Title.ShouldBe("Falha de autenticação");
    }

    [Fact]
    public void IdempotencyConflictGetsItsOwnDefaultCodeWhenTheBodyHasNone()
    {
        var exception = new KiraIdempotencyConflictException(HttpStatusCode.Conflict, errorCode: null, "conflito", responseBody: null);

        exception.ToSingleProblemDetail().Code.ShouldBe("IDEMPOTENCY_CONFLICT");
    }

    [Fact]
    public void EnvelopeWithProblemDetailsRoundTripsThroughEnvelopeJson()
    {
        var exception = new KiraApiException(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "erro", null);
        var envelope = Envelope.Fail<object>(
            exception.ToProblemDetails(), "VALIDATION_ERROR", "Corpo inválido.");

        var json = System.Text.Json.JsonSerializer.Serialize(envelope, CambioReal.Contracts.Serialization.EnvelopeJson.Options);

        json.ShouldContain("\"success\":false");
        json.ShouldContain("\"code\":\"VALIDATION_ERROR\"");
        json.ShouldContain("\"retryable\":false");
        json.ShouldContain("\"severity\":\"error\"");
        json.ShouldNotContain("\"data\":{}");
    }
}
