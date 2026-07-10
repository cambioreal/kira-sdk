namespace CambioReal.Kira.Http;

internal static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Copia a requisição para que ela possa ser reenviada.
    /// </summary>
    /// <remarks>
    /// Uma <see cref="HttpRequestMessage"/> não pode ser enviada duas vezes, e o <see cref="HttpClient"/>
    /// descarta o <see cref="HttpRequestMessage.Content"/> assim que o envio termina. Logo, a cópia precisa
    /// ser feita <em>antes</em> do primeiro envio, o que implica bufferizar o corpo em memória.
    /// Para os payloads da Kira que carregam documentos em base64 (payout crypto), isso dobra
    /// temporariamente o uso de memória daquela requisição.
    /// </remarks>
    public static async Task<HttpRequestMessage> CloneAsync(this HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy,
        };

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = new ByteArrayContent(body);

            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in (IDictionary<string, object?>)request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
