using System.Net;
using CambioReal.Kira.Auth;
using Microsoft.Extensions.Options;

namespace CambioReal.Kira.Tests.Fakes;

internal static class TestClient
{
    /// <summary>Monta um <see cref="KiraClient"/> sobre um transporte gravado, com token fixo.</summary>
    public static (KiraClient Client, RecordingHttpMessageHandler Transport) Create(
        params (HttpStatusCode Status, string Json)[] responses)
    {
        var transport = new RecordingHttpMessageHandler();

        foreach (var (status, json) in responses)
        {
            transport.RespondWith(status, json);
        }

        var options = ConfigurationTests.NewOptions();

        var authHandler = new KiraAuthenticationHandler(new StubTokenProvider("tok-1"), Options.Create(options))
        {
            InnerHandler = transport,
        };

        var client = new KiraClient(new HttpClient(authHandler) { BaseAddress = options.ResolveBaseAddress() });
        return (client, transport);
    }

    /// <summary>Resposta 200 vazia, para quando só interessa o que foi enviado.</summary>
    public static (KiraClient Client, RecordingHttpMessageHandler Transport) CreateOk(string json = "{}") =>
        Create((HttpStatusCode.OK, json));
}
