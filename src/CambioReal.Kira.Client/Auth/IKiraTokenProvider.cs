namespace CambioReal.Kira.Auth;

/// <summary>Fornece tokens JWT de acesso, com cache.</summary>
public interface IKiraTokenProvider
{
    /// <summary>
    /// Devolve um token válido, reautenticando se necessário.
    /// </summary>
    /// <param name="invalidatedToken">
    /// O token que acabou de ser rejeitado com 401, ou <see langword="null"/> numa chamada normal.
    /// Quando informado, o provedor só reautentica se o valor em cache ainda for esse mesmo token —
    /// assim, várias requisições que tomam 401 em paralelo compartilham uma única reautenticação
    /// em vez de dispararem uma cada.
    /// </param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public ValueTask<string> GetAccessTokenAsync(string? invalidatedToken, CancellationToken cancellationToken = default);
}
