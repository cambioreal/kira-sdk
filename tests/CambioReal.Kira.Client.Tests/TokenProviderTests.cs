using System.Net;
using CambioReal.Kira.Auth;
using CambioReal.Kira.Http;
using CambioReal.Kira.Tests.Fakes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

public sealed class TokenProviderTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuthenticatesOnceAndCachesTheToken()
    {
        var (provider, transport) = Build(new MutableTimeProvider(Epoch), TokenResponse("tok-1"));

        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");
        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");

        transport.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AuthRequestCarriesApiKeyAndSnakeCaseBody()
    {
        var (provider, transport) = Build(new MutableTimeProvider(Epoch), TokenResponse("tok-1"));

        await provider.GetAccessTokenAsync(null);

        var request = transport.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.ToString().ShouldBe("https://api.balampay.com/sandbox/auth");
        request.ApiKey.ShouldBe("api-key");
        request.Authorization.ShouldBeNull();
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"client_id\"");
        request.Body.ShouldContain("\"password\"");
    }

    [Fact]
    public async Task RenewsAfterExpiryMinusSkew()
    {
        var clock = new MutableTimeProvider(Epoch);
        var (provider, transport) = Build(clock, TokenResponse("tok-1"), TokenResponse("tok-2"));

        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");

        // expires_in = 3600, skew = 60 → o token vale até Epoch + 3540s.
        clock.Advance(TimeSpan.FromSeconds(3539));
        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");

        clock.Advance(TimeSpan.FromSeconds(2));
        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-2");

        transport.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task InvalidatedTokenForcesRenewalEvenIfNotExpired()
    {
        var (provider, transport) = Build(new MutableTimeProvider(Epoch), TokenResponse("tok-1"), TokenResponse("tok-2"));

        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");
        (await provider.GetAccessTokenAsync("tok-1")).ShouldBe("tok-2");

        transport.Requests.Count.ShouldBe(2);
    }

    /// <summary>
    /// Duas requisições tomam 401 com o mesmo token. A segunda a chegar deve reaproveitar a
    /// renovação da primeira em vez de disparar um segundo <c>POST /auth</c>.
    /// </summary>
    [Fact]
    public async Task ConcurrentInvalidationsShareASingleRefresh()
    {
        var (provider, transport) = Build(new MutableTimeProvider(Epoch), TokenResponse("tok-1"), TokenResponse("tok-2"));

        await provider.GetAccessTokenAsync(null);

        var refreshed = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => provider.GetAccessTokenAsync("tok-1").AsTask()));

        refreshed.ShouldAllBe(token => token == "tok-2");
        transport.Requests.Count.ShouldBe(2); // 1 inicial + 1 renovação compartilhada
    }

    [Fact]
    public async Task SkewNeverExceedsTokenLifetime()
    {
        var options = ConfigurationTests.NewOptions();
        options.TokenExpirationSkew = TimeSpan.FromSeconds(600);

        var clock = new MutableTimeProvider(Epoch);

        // expires_in menor que o skew: sem a proteção, o token nasceria vencido e o provedor
        // reautenticaria a cada chamada.
        var (provider, transport) = Build(clock, options, TokenResponse("tok-1", expiresIn: 60));

        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");

        clock.Advance(TimeSpan.FromSeconds(29));
        (await provider.GetAccessTokenAsync(null)).ShouldBe("tok-1");

        transport.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FailedAuthenticationThrows()
    {
        var transport = new RecordingHttpMessageHandler();
        transport.RespondWith(HttpStatusCode.Forbidden, """{"code":"INVALID_CLIENT"}""");

        var provider = NewProvider(transport, ConfigurationTests.NewOptions(), new MutableTimeProvider(Epoch));

        var error = await Should.ThrowAsync<KiraAuthenticationException>(
            async () => await provider.GetAccessTokenAsync(null));

        error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static string TokenResponse(string token, int expiresIn = 3600) =>
        $$"""{"access_token":"{{token}}","expires_in":{{expiresIn}},"token_type":"Bearer"}""";

    private static (IKiraTokenProvider Provider, RecordingHttpMessageHandler Transport) Build(
        TimeProvider clock,
        params string[] responses) =>
        Build(clock, ConfigurationTests.NewOptions(), responses);

    private static (IKiraTokenProvider Provider, RecordingHttpMessageHandler Transport) Build(
        TimeProvider clock,
        KiraOptions options,
        params string[] responses)
    {
        var transport = new RecordingHttpMessageHandler();

        foreach (var response in responses)
        {
            transport.RespondWith(HttpStatusCode.OK, response);
        }

        return (NewProvider(transport, options, clock), transport);
    }

    private static KiraTokenProvider NewProvider(RecordingHttpMessageHandler transport, KiraOptions options, TimeProvider clock) =>
        new(new SingleHandlerHttpClientFactory(transport, options.ResolveBaseAddress()), Options.Create(options), clock);
}
