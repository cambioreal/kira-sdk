using CambioReal.Kira.Auth;

namespace CambioReal.Kira.Tests.Fakes;

/// <summary>Devolve tokens numa sequência fixa e grava cada pedido de invalidação.</summary>
internal sealed class StubTokenProvider(params string[] tokens) : IKiraTokenProvider
{
    private readonly List<string?> invalidations = [];
    private int cursor;

    public IReadOnlyList<string?> Invalidations => invalidations;

    public ValueTask<string> GetAccessTokenAsync(string? invalidatedToken, CancellationToken cancellationToken = default)
    {
        invalidations.Add(invalidatedToken);

        // Só avança quando um token é explicitamente invalidado; caso contrário devolve o atual.
        if (invalidatedToken is not null && cursor < tokens.Length - 1)
        {
            cursor++;
        }

        return ValueTask.FromResult(tokens[cursor]);
    }
}

/// <summary>Fábrica que entrega sempre o mesmo transporte de teste.</summary>
internal sealed class SingleHandlerHttpClientFactory(HttpMessageHandler handler, Uri baseAddress) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) =>
        new(handler, disposeHandler: false) { BaseAddress = baseAddress };
}
