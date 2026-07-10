using System.Net;
using CambioReal.Kira.Auth;
using CambioReal.Kira.Http;
using CambioReal.Kira.Tests.Fakes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

public sealed class KiraClientTests
{
    private sealed record Probe(string Id);

    private sealed record CreateProbe(string BusinessLegalName);

    [Fact]
    public async Task SendsApiKeyAndBearerOnEveryRequest()
    {
        var (client, transport, _) = Build(("tok-1", HttpStatusCode.OK, """{"id":"u-1"}"""));

        await client.GetAsync<Probe>("v1/users/u-1");

        var request = transport.Requests.Single();
        request.ApiKey.ShouldBe("api-key");
        request.Authorization.ShouldBe("Bearer tok-1");
        request.RequestUri!.ToString().ShouldBe("https://api.balampay.com/sandbox/v1/users/u-1");
    }

    [Fact]
    public async Task LeadingSlashInPathIsRejected()
    {
        var (client, _, _) = Build(("tok-1", HttpStatusCode.OK, "{}"));

        var error = await Should.ThrowAsync<ArgumentException>(
            async () => await client.GetAsync<Probe>("/v1/users"));

        error.Message.ShouldContain("não pode começar com '/'");
    }

    [Fact]
    public async Task IdempotencyKeyAndOtpAreForwarded()
    {
        var (client, transport, _) = Build(("tok-1", HttpStatusCode.Created, """{"id":"p-1"}"""));

        var key = Guid.NewGuid();
        var context = new KiraRequestContext { IdempotencyKey = key, ValidationCode = "123456" };

        await client.PostAsync<CreateProbe, Probe>("v1/payouts", new CreateProbe("Acme"), context);

        var request = transport.Requests.Single();
        request.IdempotencyKey.ShouldBe(key.ToString("D"));
        request.ValidationCode.ShouldBe("123456");
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"business_legal_name\"");
    }

    [Fact]
    public async Task SkipBearerSendsOnlyApiKey()
    {
        var (client, transport, _) = Build(("tok-1", HttpStatusCode.OK, """{"id":"w-1"}"""));

        await client.PostAsync<CreateProbe, Probe>(
            "webhooks/register",
            new CreateProbe("Acme"),
            new KiraRequestContext { SkipBearerAuthentication = true });

        var request = transport.Requests.Single();
        request.ApiKey.ShouldBe("api-key");
        request.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task UnauthorizedTriggersExactlyOneRefreshAndReplaysTheBody()
    {
        var transport = new RecordingHttpMessageHandler();
        transport.RespondWith(HttpStatusCode.Unauthorized, """{"message":"The incoming token has expired"}""");
        transport.RespondWith(HttpStatusCode.Created, """{"id":"u-1"}""");

        var tokenProvider = new StubTokenProvider("tok-1", "tok-2");
        var client = NewClient(transport, tokenProvider);

        var result = await client.PostAsync<CreateProbe, Probe>(
            "v1/users",
            new CreateProbe("Acme"),
            KiraRequestContext.WithNewIdempotencyKey());

        result.Id.ShouldBe("u-1");
        transport.Requests.Count.ShouldBe(2);

        transport.Requests[0].Authorization.ShouldBe("Bearer tok-1");
        transport.Requests[1].Authorization.ShouldBe("Bearer tok-2");

        // O corpo e a chave de idempotência precisam sobreviver ao replay — senão o retry
        // criaria um segundo usuário em vez de reaproveitar a requisição original.
        transport.Requests[1].Body.ShouldBe(transport.Requests[0].Body);
        transport.Requests[1].IdempotencyKey.ShouldBe(transport.Requests[0].IdempotencyKey);

        tokenProvider.Invalidations.ShouldBe([null, "tok-1"]);
    }

    [Fact]
    public async Task SecondUnauthorizedIsNotRetriedAgain()
    {
        var transport = new RecordingHttpMessageHandler();
        transport.RespondWith(HttpStatusCode.Unauthorized, """{"code":"UNAUTHORIZED"}""");
        transport.RespondWith(HttpStatusCode.Unauthorized, """{"code":"UNAUTHORIZED"}""");

        var client = NewClient(transport, new StubTokenProvider("tok-1", "tok-2"));

        var error = await Should.ThrowAsync<KiraAuthenticationException>(
            async () => await client.GetAsync<Probe>("v1/users"));

        error.ErrorCode.ShouldBe("UNAUTHORIZED");
        transport.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ConflictBecomesIdempotencyConflictException()
    {
        var (client, _, _) = Build(("tok-1", HttpStatusCode.Conflict,
            """{"code":"IDEMPOTENCY_CONFLICT","message":"This idempotency key has been used with different request data"}"""));

        var error = await Should.ThrowAsync<KiraIdempotencyConflictException>(
            async () => await client.PostAsync<CreateProbe, Probe>(
                "v1/users",
                new CreateProbe("Acme"),
                KiraRequestContext.WithNewIdempotencyKey()));

        error.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        error.ErrorCode.ShouldBe("IDEMPOTENCY_CONFLICT");
        error.ResponseBody.ShouldNotBeNull();
    }

    [Fact]
    public async Task ValidationErrorSurfacesTheKiraErrorCode()
    {
        var (client, _, _) = Build(("tok-1", HttpStatusCode.BadRequest, """{"code":"VALIDATION_REQUIRED"}"""));

        var error = await Should.ThrowAsync<KiraApiException>(
            async () => await client.GetAsync<Probe>("v1/payouts"));

        error.ErrorCode.ShouldBe("VALIDATION_REQUIRED");
        error.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NonJsonErrorBodyDoesNotMaskTheStatus()
    {
        var transport = new RecordingHttpMessageHandler();
        transport.RespondWith(HttpStatusCode.BadGateway, "<html>502 Bad Gateway</html>");

        var client = NewClient(transport, new StubTokenProvider("tok-1"));

        var error = await Should.ThrowAsync<KiraApiException>(
            async () => await client.GetAsync<Probe>("v1/users"));

        error.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        error.ErrorCode.ShouldBeNull();
    }

    private static (KiraClient Client, RecordingHttpMessageHandler Transport, StubTokenProvider Tokens) Build(
        params (string Token, HttpStatusCode Status, string Json)[] responses)
    {
        var transport = new RecordingHttpMessageHandler();

        foreach (var (_, status, json) in responses)
        {
            transport.RespondWith(status, json);
        }

        var tokens = new StubTokenProvider(responses.Select(r => r.Token).Distinct().ToArray());
        return (NewClient(transport, tokens), transport, tokens);
    }

    private static KiraClient NewClient(RecordingHttpMessageHandler transport, IKiraTokenProvider tokenProvider)
    {
        var options = ConfigurationTests.NewOptions();

        var authHandler = new KiraAuthenticationHandler(tokenProvider, Options.Create(options))
        {
            InnerHandler = transport,
        };

        return new KiraClient(new HttpClient(authHandler) { BaseAddress = options.ResolveBaseAddress() });
    }
}
