namespace CambioReal.Kira.Auth;

/// <summary>Corpo de <c>POST /auth</c>. Serializado como <c>client_id</c> / <c>password</c>.</summary>
internal sealed record AuthTokenRequest(string ClientId, string Password);

/// <summary>Resposta de <c>POST /auth</c>: <c>access_token</c>, <c>expires_in</c>, <c>token_type</c>.</summary>
internal sealed record AuthTokenResponse(string AccessToken, int ExpiresIn, string TokenType);

/// <summary>
/// Envelope real de <c>POST /auth</c>: <c>{"message": "...", "data": {access_token, expires_in, token_type}}</c>.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-13 — a doc em prosa não menciona o envelope, só os
/// campos internos. Sem desempacotar <see cref="Data"/>, todo campo lido na raiz vem vazio.
/// </remarks>
internal sealed record AuthTokenEnvelope(AuthTokenResponse? Data);

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
