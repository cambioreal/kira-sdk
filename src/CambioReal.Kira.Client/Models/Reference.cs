using System.Text.Json.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/payment-link</c>.
/// </summary>
/// <remarks>
/// <b>Reescrito em 2026-07-14</b> a partir de uma criação real bem-sucedida contra o sandbox
/// (<c>country_code=BR</c>/<c>currency=BRL</c> — <c>country_code=US</c> falha à parte, com
/// <c>acct_type must be USD for US recipients</c>, aparentemente uma regra de negócio do
/// recipient/conta, não um campo de request faltante). Faltavam três campos obrigatórios
/// inteiros: <see cref="ClientUuid"/>, <see cref="Reference"/> (existia, mas era opcional) e
/// <see cref="CountryCode"/>.
/// </remarks>
public sealed record CreatePaymentLinkRequest
{
    /// <summary>
    /// Usuário que solicita o pagamento. Não confirmado como campo reconhecido pela API — nunca
    /// apareceu em nenhuma mensagem de erro de validação, mesmo quando ausente. Mantido por não
    /// ter sido comprovadamente descartado.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Identificador do seu client Kira (o mesmo <see cref="KiraOptions.ClientId"/> usado para
    /// autenticar). Confirmado obrigatório contra o sandbox em 2026-07-14 — passar outro valor
    /// (ex.: um <c>user_id</c>) falha com <c>PL_AUTH_001 Client UUID mismatch</c>.
    /// </summary>
    public required string ClientUuid { get; init; }

    /// <summary>Valor solicitado.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public decimal? Amount { get; init; }

    /// <summary>
    /// Moeda. A doc dizia "sempre USD", mas <c>BRL</c> foi aceito e criou o link com sucesso
    /// contra o sandbox em 2026-07-14 — não é USD-only.
    /// </summary>
    public Currency? Currency { get; init; }

    /// <summary>
    /// País do recipient, ISO 3166-1 <b>alpha-2</b> (não alpha-3 como o resto do SDK — confirmado
    /// contra o sandbox: <c>"USA"</c> falha com <c>country_code must be 2 characters</c>).
    /// Confirmado obrigatório em 2026-07-14. <c>US</c> tem uma restrição de negócio adicional
    /// não resolvida (ver <see cref="CreatePaymentLinkRequest"/>).
    /// </summary>
    public required string CountryCode { get; init; }

    /// <summary>Destino após o pagamento.</summary>
    public Uri? RedirectUri { get; init; }

    /// <summary>Descrição apresentada ao pagador.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Sua referência interna. Confirmado obrigatório contra o sandbox em 2026-07-14 — antes
    /// desta versão era opcional.
    /// </summary>
    public required string Reference { get; init; }
}

/// <summary>
/// Payment link criado. Confirmado contra o sandbox em 2026-07-14 (criação real bem-sucedida).
/// </summary>
public sealed record KiraPaymentLink : KiraResponse
{
    /// <summary>Identificador do link. Campo real é <c>txn_uuid</c>, não <c>id</c> — não existe campo <c>id</c> na resposta.</summary>
    [JsonPropertyName("txn_uuid")]
    public string Id { get; init; } = string.Empty;

    /// <summary>URL a apresentar ao pagador. Campo real é <c>payment_link</c>, não <c>url</c>.</summary>
    [JsonPropertyName("payment_link")]
    public Uri? Url { get; init; }

    /// <summary>Situação. Valor observado na criação: <c>INIT</c>.</summary>
    public string? Status { get; init; }

    /// <summary>Forma de pagamento. Valor observado: <c>CASH</c>. Campo novo, não documentado.</summary>
    public string? PaymentType { get; init; }

    /// <summary>Eco de <see cref="CreatePaymentLinkRequest.ClientUuid"/>.</summary>
    public string? ClientUuid { get; init; }

    /// <summary>Eco de <see cref="CreatePaymentLinkRequest.Reference"/>.</summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Valor. Não confirmado presente na resposta de criação — <c>amount</c>/<c>currency</c> não
    /// vieram de volta no teste contra o sandbox, apesar de enviados no request.
    /// </summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Amount { get; init; }

    /// <summary>
    /// Expiração. Não confirmado presente na resposta de criação — não observado no sandbox.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Criação. Confirmado contra o sandbox em 2026-07-14 — ISO 8601 padrão aqui (diferente do
    /// formato não padrão de <see cref="KiraRecipient.CreatedAt"/>).
    /// </summary>
    [JsonPropertyName("created_ts")]
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Última atualização.</summary>
    [JsonPropertyName("updated_ts")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Corpo de <c>POST /webhooks/register</c>.
/// </summary>
/// <remarks>
/// <b>Reescrito em 2026-07-14</b> a partir de um registro real bem-sucedido contra o sandbox
/// (<c>200</c>). A doc e o código diziam que este endpoint autentica só com <c>x-api-key</c>
/// (sem Bearer) — errado: sem Bearer, a API devolve <c>401</c> disfarçado de "rota não
/// corresponde". Ver <see cref="Resources.PlatformResource.RegisterWebhookAsync"/>, que não usa
/// mais <c>SkipBearerAuthentication</c>. O campo da URL também tinha o nome errado.
/// </remarks>
public sealed record RegisterWebhookRequest
{
    /// <summary>
    /// Identificador do seu client Kira (o mesmo <see cref="KiraOptions.ClientId"/> usado para
    /// autenticar). Confirmado obrigatório contra o sandbox em 2026-07-14.
    /// </summary>
    public required string ClientUuid { get; init; }

    /// <summary>
    /// Endpoint que receberá os eventos. Precisa ser HTTPS. Campo real é <c>webhook_url</c>, não
    /// <c>url</c> — confirmado contra o sandbox em 2026-07-14.
    /// </summary>
    public required Uri WebhookUrl { get; init; }

    /// <summary>
    /// Segredo usado para assinar os eventos em HMAC SHA-256. Fortemente recomendado.
    /// Guarde-o no <c>pass</c> e verifique com <see cref="Webhooks.KiraWebhookVerifier"/>.
    /// </summary>
    public string? SecretKey { get; init; }
}

/// <summary>
/// Confirmação de registro do webhook.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14: a resposta de um registro bem-sucedido é só
/// <c>{"message": "Webhook registered successfully"}</c> — sem identificador, sem eco da URL.
/// Consistente com a doc: não existe endpoint para listar ou remover webhooks depois.
/// </remarks>
public sealed record KiraWebhookRegistration : KiraResponse
{
    /// <summary>Mensagem de confirmação.</summary>
    public string? Message { get; init; }
}

/// <summary>Subdivisão (estado ou província) de um país.</summary>
public sealed record CountrySubdivision : KiraResponse
{
    /// <summary>Código da subdivisão.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nome da subdivisão.</summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>País suportado para endereços de usuário.</summary>
public sealed record KiraCountry : KiraResponse
{
    /// <summary>
    /// Código ISO 3166-1 alpha-3. Confirmado contra o sandbox em 2026-07-13: o campo real se
    /// chama <c>alpha3</c>, não <c>code</c> — sem o mapeamento explícito, este campo vinha sempre vazio.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("alpha3")]
    public string Code { get; init; } = string.Empty;

    /// <summary>Nome do país.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Estados ou províncias.</summary>
    public IReadOnlyList<CountrySubdivision> Subdivisions { get; init; } = [];
}

/// <summary>
/// Envelope real de <c>GET /v1/countries</c>: <c>{"count": N, "data": [...]}</c>.
/// </summary>
/// <remarks>Confirmado contra o sandbox em 2026-07-13 — a lista não vem como array na raiz.</remarks>
internal sealed record KiraCountriesEnvelope(int? Count, IReadOnlyList<KiraCountry> Data);

/// <summary>Banco disponível para operações de pagamento em um país.</summary>
public sealed record KiraBank : KiraResponse
{
    /// <summary>Código do banco, usado em <see cref="CreatePayInRequest.BankCode"/>.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nome do banco.</summary>
    public string Name { get; init; } = string.Empty;
}
