namespace CambioReal.Kira.Auth;

/// <summary>Corpo de <c>POST /auth</c>. Serializado como <c>client_id</c> / <c>password</c>.</summary>
internal sealed record AuthTokenRequest(string ClientId, string Password);

/// <summary>Resposta de <c>POST /auth</c>: <c>access_token</c>, <c>expires_in</c>, <c>token_type</c>.</summary>
internal sealed record AuthTokenResponse(string AccessToken, int ExpiresIn, string TokenType);

/// <summary>Token em cache com seu instante de expiração absoluto.</summary>
internal sealed record CachedAccessToken(string Value, DateTimeOffset ExpiresAtUtc);

/// <summary>Nomes dos <c>HttpClient</c> registrados no container.</summary>
internal static class KiraClientNames
{
    /// <summary>Cliente da API. Passa pelo <see cref="KiraAuthenticationHandler"/>.</summary>
    public const string Api = "kira.api";

    /// <summary>
    /// Cliente usado só para <c>POST /auth</c>. Precisa ser separado do <see cref="Api"/>:
    /// se passasse pelo handler de autenticação, obter um token exigiria um token.
    /// </summary>
    public const string Auth = "kira.auth";
}
