using System.Net.Http.Json;
using CambioReal.Kira.Http;
using CambioReal.Kira.Serialization;
using Microsoft.Extensions.Options;

namespace CambioReal.Kira.Auth;

/// <summary>
/// Cacheia o JWT da Kira e o renova sob demanda.
/// </summary>
/// <remarks>
/// A Kira emite um token de 3600 s e <em>não</em> oferece refresh token: a única forma de renovar é
/// repetir o <c>POST /auth</c>. Este provedor é singleton e serializa as renovações concorrentes
/// (single-flight), para que uma rajada de requisições após a expiração produza uma reautenticação,
/// não N.
/// </remarks>
internal sealed class KiraTokenProvider : IKiraTokenProvider, IDisposable
{
    private readonly IHttpClientFactory httpClientFactory;
    private readonly KiraOptions options;
    private readonly TimeProvider timeProvider;
    private readonly SemaphoreSlim refreshGate = new(1, 1);

    private CachedAccessToken? cachedToken;

    public KiraTokenProvider(IHttpClientFactory httpClientFactory, IOptions<KiraOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.httpClientFactory = httpClientFactory;
        this.options = options.Value;
        this.timeProvider = timeProvider;
    }

    public async ValueTask<string> GetAccessTokenAsync(string? invalidatedToken, CancellationToken cancellationToken = default)
    {
        if (TryUseCached(Volatile.Read(ref cachedToken), invalidatedToken, out var token))
        {
            return token;
        }

        await refreshGate.WaitAsync(cancellationToken);
        try
        {
            // Outra thread pode ter renovado enquanto esperávamos o semáforo.
            if (TryUseCached(Volatile.Read(ref cachedToken), invalidatedToken, out token))
            {
                return token;
            }

            var fresh = await RequestTokenAsync(cancellationToken);
            Volatile.Write(ref cachedToken, fresh);
            return fresh.Value;
        }
        finally
        {
            refreshGate.Release();
        }
    }

    public void Dispose() => refreshGate.Dispose();

    private bool TryUseCached(CachedAccessToken? current, string? invalidatedToken, out string token)
    {
        token = string.Empty;

        if (current is null)
        {
            return false;
        }

        // Se este é exatamente o token que a API acabou de rejeitar, ele não serve — mesmo que
        // o relógio diga que ainda não expirou.
        if (invalidatedToken is not null && string.Equals(current.Value, invalidatedToken, StringComparison.Ordinal))
        {
            return false;
        }

        if (timeProvider.GetUtcNow() >= current.ExpiresAtUtc)
        {
            return false;
        }

        token = current.Value;
        return true;
    }

    private async Task<CachedAccessToken> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient(KiraClientNames.Auth);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("auth", UriKind.Relative))
        {
            Content = JsonContent.Create(new AuthTokenRequest(options.ClientId, options.Password), options: KiraJson.Options),
        };

        // O /auth é o único endpoint que se autentica só com a api key.
        request.Headers.TryAddWithoutValidation(KiraHeaders.ApiKey, options.ApiKey);

        // O instante é capturado antes do envio: se a rede demorar, preferimos subestimar a
        // validade restante a superestimá-la.
        var issuedAt = timeProvider.GetUtcNow();

        using var response = await client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new KiraAuthenticationException(
                response.StatusCode,
                errorCode: null,
                $"Falha ao autenticar na Kira (HTTP {(int)response.StatusCode}).",
                body);
        }

        var envelope = await response.Content.ReadFromJsonAsync<AuthTokenEnvelope>(KiraJson.Options, cancellationToken);
        var payload = envelope?.Data
            ?? throw new KiraAuthenticationException("A Kira devolveu um corpo vazio em POST /auth.");

        if (string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new KiraAuthenticationException("A Kira devolveu um access_token vazio.");
        }

        var lifetime = TimeSpan.FromSeconds(payload.ExpiresIn);
        var skew = options.TokenExpirationSkew;

        // Nunca deixar a margem consumir toda a vida do token: um expires_in curto com skew grande
        // produziria um token já vencido no instante da emissão, gerando um laço de renovação.
        if (skew >= lifetime)
        {
            skew = TimeSpan.FromTicks(lifetime.Ticks / 2);
        }

        return new CachedAccessToken(payload.AccessToken, issuedAt + lifetime - skew);
    }
}
