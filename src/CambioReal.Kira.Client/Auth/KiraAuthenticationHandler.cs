using System.Net;
using System.Net.Http.Headers;
using CambioReal.Kira.Http;
using Microsoft.Extensions.Options;

namespace CambioReal.Kira.Auth;

/// <summary>
/// Injeta <c>x-api-key</c> em toda requisição e <c>Authorization: Bearer</c> onde for exigido,
/// reautenticando uma única vez diante de um 401.
/// </summary>
internal sealed class KiraAuthenticationHandler : DelegatingHandler
{
    private const string BearerScheme = "Bearer";

    private readonly IKiraTokenProvider tokenProvider;
    private readonly KiraOptions options;

    public KiraAuthenticationHandler(IKiraTokenProvider tokenProvider, IOptions<KiraOptions> options)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ArgumentNullException.ThrowIfNull(options);

        this.tokenProvider = tokenProvider;
        this.options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A Kira exige x-api-key em todos os endpoints, inclusive naqueles que dispensam o JWT.
        request.Headers.Remove(KiraHeaders.ApiKey);
        request.Headers.TryAddWithoutValidation(KiraHeaders.ApiKey, options.ApiKey);

        if (request.Options.TryGetValue(KiraRequestOptionKeys.SkipBearerAuthentication, out var skipBearer) && skipBearer)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var token = await tokenProvider.GetAccessTokenAsync(invalidatedToken: null, cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);

        // A cópia precisa existir antes do envio — depois dele o Content já foi descartado.
        var retry = await request.CloneAsync(cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            retry.Dispose();
            return response;
        }

        // Um 401 aqui significa token expirado ou revogado. Renovamos e tentamos de novo, uma vez.
        // Se o segundo 401 vier, ele sobe para o KiraClient, que o traduz em KiraAuthenticationException —
        // credencial errada não se resolve com retry.
        response.Dispose();

        var refreshedToken = await tokenProvider.GetAccessTokenAsync(invalidatedToken: token, cancellationToken);
        retry.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, refreshedToken);

        return await base.SendAsync(retry, cancellationToken);
    }
}
