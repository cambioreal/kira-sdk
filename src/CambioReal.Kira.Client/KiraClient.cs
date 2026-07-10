using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CambioReal.Kira.Http;
using CambioReal.Kira.Resources;
using CambioReal.Kira.Serialization;

namespace CambioReal.Kira;

/// <summary>
/// Cliente HTTP da API Kira.
/// </summary>
/// <remarks>
/// Esta é a camada de transporte: autenticação, idempotência, OTP e tradução de erros.
/// Os recursos tipados (<c>users</c>, <c>virtual-accounts</c>, <c>payouts</c>, …) ainda não estão
/// modelados porque a Kira não publica um OpenAPI e a documentação descreve os payloads em prosa,
/// com contradições conhecidas. Use <see cref="GetAsync{TResponse}"/> e
/// <see cref="PostAsync{TRequest, TResponse}"/> com seus próprios contratos até que o probe
/// contra o sandbox confirme os schemas.
/// </remarks>
public sealed class KiraClient
{
    private readonly HttpClient httpClient;

    /// <summary>Cria o cliente sobre um <see cref="HttpClient"/> já configurado.</summary>
    public KiraClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.httpClient = httpClient;

        Users = new UsersResource(this);
        Recipients = new RecipientsResource(this);
        VirtualAccounts = new VirtualAccountsResource(this);
        PayIns = new PayInsResource(this);
        PaymentLinks = new PaymentLinksResource(this);
        Platform = new PlatformResource(this);
    }

    /// <summary>Usuários, verificação e elegibilidade por produto.</summary>
    public UsersResource Users { get; }

    /// <summary>Destinatários de payout.</summary>
    public RecipientsResource Recipients { get; }

    /// <summary>Contas virtuais, depósitos, payouts e endereços de liquidação.</summary>
    public VirtualAccountsResource VirtualAccounts { get; }

    /// <summary>Coleta de pagamentos via PSE e SPEI.</summary>
    public PayInsResource PayIns { get; }

    /// <summary>Payment links.</summary>
    public PaymentLinksResource PaymentLinks { get; }

    /// <summary>Webhooks, países, bancos e emissão de OTP.</summary>
    public PlatformResource Platform { get; }

    /// <summary>Executa um GET e desserializa a resposta.</summary>
    /// <param name="path">Path relativo, sem barra inicial (ex.: <c>v1/countries</c>).</param>
    /// <param name="context">Modificadores da requisição.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task<TResponse> GetAsync<TResponse>(
        string path,
        KiraRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, path, context ?? KiraRequestContext.Default, content: null);
        return await SendAndReadAsync<TResponse>(request, cancellationToken);
    }

    /// <summary>Executa um POST com corpo JSON e desserializa a resposta.</summary>
    /// <param name="path">Path relativo, sem barra inicial (ex.: <c>v1/users</c>).</param>
    /// <param name="body">Corpo da requisição.</param>
    /// <param name="context">Modificadores da requisição. Endpoints de criação exigem <see cref="KiraRequestContext.IdempotencyKey"/>.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        KiraRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(body, options: KiraJson.Options);
        using var request = CreateRequest(HttpMethod.Post, path, context ?? KiraRequestContext.Default, content);
        return await SendAndReadAsync<TResponse>(request, cancellationToken);
    }

    /// <summary>Executa um PATCH com corpo JSON e desserializa a resposta.</summary>
    /// <param name="path">Path relativo, sem barra inicial.</param>
    /// <param name="body">Campos a alterar. Só o que for enviado muda.</param>
    /// <param name="context">Modificadores da requisição.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    public async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        KiraRequestContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var content = JsonContent.Create(body, options: KiraJson.Options);
        using var request = CreateRequest(HttpMethod.Patch, path, context ?? KiraRequestContext.Default, content);
        return await SendAndReadAsync<TResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Escape hatch: envia uma requisição arbitrária pelo pipeline autenticado.
    /// O chamador é dono da resposta e deve descartá-la.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        KiraRequestContext context,
        HttpContent? content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(context);

        // Um path com barra inicial é resolvido contra a raiz do host e descartaria o prefixo
        // /sandbox — mandando uma requisição de teste para produção.
        if (path.StartsWith('/'))
        {
            throw new ArgumentException(
                $"O path deve ser relativo e não pode começar com '/'. Recebido: '{path}'.",
                nameof(path));
        }

        var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative))
        {
            Content = content,
        };

        if (context.IdempotencyKey is { } idempotencyKey)
        {
            request.Headers.TryAddWithoutValidation(
                KiraHeaders.IdempotencyKey,
                idempotencyKey.ToString("D", CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrEmpty(context.ValidationCode))
        {
            request.Headers.TryAddWithoutValidation(KiraHeaders.ValidationCode, context.ValidationCode);
        }

        if (context.SkipBearerAuthentication)
        {
            request.Options.Set(KiraRequestOptionKeys.SkipBearerAuthentication, true);
        }

        return request;
    }

    private async Task<TResponse> SendAndReadAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfUnsuccessfulAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TResponse>(KiraJson.Options, cancellationToken);

        return payload ?? throw new KiraApiException(
            response.StatusCode,
            errorCode: null,
            "A API Kira devolveu um corpo JSON vazio onde um objeto era esperado.",
            responseBody: null);
    }

    private static async Task ThrowIfUnsuccessfulAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var errorCode = TryExtractErrorCode(body);
        var message = $"A API Kira respondeu HTTP {(int)response.StatusCode} ({response.StatusCode})"
            + (errorCode is null ? "." : $": {errorCode}.");

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new KiraAuthenticationException(response.StatusCode, errorCode, message, body),
            HttpStatusCode.Conflict => new KiraIdempotencyConflictException(response.StatusCode, errorCode, message, body),
            _ => new KiraApiException(response.StatusCode, errorCode, message, body),
        };
    }

    /// <summary>
    /// Extrai o código de erro do corpo. A Kira não documenta um envelope de erro único, então
    /// tentamos os nomes de campo observados (<c>code</c>, <c>error_code</c>, <c>error</c>).
    /// </summary>
    private static string? TryExtractErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var candidate in (ReadOnlySpan<string>)["code", "error_code", "error"])
            {
                if (document.RootElement.TryGetProperty(candidate, out var value) && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Corpo não-JSON (ex.: HTML de um proxy). O status e o corpo bruto já vão na exceção.
        }

        return null;
    }
}
