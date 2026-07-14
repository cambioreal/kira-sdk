# CambioReal.Kira.Client

Cliente .NET tipado para a [Kira Financial AI API](https://kira-financial-ai.readme.io) — recebimento, custódia e pagamento de transações internacionais.

Consumidor primário: [`cambioreal/cambio-real-v3`](https://github.com/cambioreal/cambio-real-v3). Alvo `net10.0`, SDK fixada em `10.0.301`.

## Estado

Os **27 endpoints documentados** da Kira estão implementados e tipados.

A camada de transporte cuida de autenticação JWT com cache, single-flight e renovação automática em 401; `x-api-key` em toda requisição e `Authorization: Bearer` onde a API exige; idempotência (`idempotency-key`) e OTP (`x-validation-header`); tradução de erros HTTP em exceções tipadas; e verificação de assinatura HMAC de webhooks em tempo constante.

| Recurso | Endpoints | Fachada |
|---|---|---|
| Auth | 1 | interno (`IKiraTokenProvider`) |
| Users, verificação, elegibilidade | 5 | `kira.Users` |
| Recipients | 3 | `kira.Recipients` |
| Virtual accounts, depósitos, payouts, liquidation | 11 | `kira.VirtualAccounts` |
| PayIns (PSE, SPEI) | 3 | `kira.PayIns` |
| Payment links | 1 | `kira.PaymentLinks` |
| Webhooks, países, bancos, OTP | 4 | `kira.Platform` |

> **Os modelos foram realinhados contra o sandbox real em 2026-07-13/14** (ver [Confirmado contra o sandbox](#confirmado-contra-o-sandbox-2026-07-13) para o detalhe de cada correção). Todos os pontos mapeados foram corrigidos e reverificados — a maioria com uma chamada real bem-sucedida contra o sandbox, via o próprio `KiraClient` (não só contra fakes). Isso não elimina risco: os ~18 rails de `RecipientAccount` além de `WALLET`/`SWIFT` permanecem por inferência, `CreateVirtualAccountRequest` não chegou a um `201` completo (falta KYC no usuário de teste), e as contradições de documentação originais (payout paths, OTP, quote expiry — ver tabela abaixo) não foram tocadas nesta rodada. Respostas trazem `AdditionalData` (via `[JsonExtensionData]`) para que campos não modelados não se percam, e os paths estão centralizados em `KiraPaths` para que cada correção seja de uma linha.

## Uso

```csharp
services.AddKiraClient(options =>
{
    options.ClientId    = configuration["Kira:ClientId"]!;
    options.Password    = configuration["Kira:Password"]!;
    options.ApiKey      = configuration["Kira:ApiKey"]!;
    options.Environment = KiraEnvironment.Sandbox;
});
```

```csharp
public sealed class RemittanceService(KiraClient kira)
{
    public async Task<InitiatePayoutResponse> SendAsync(string virtualAccountId, string recipientId, decimal amount)
    {
        var preview = await kira.VirtualAccounts.PreviewPayoutAsync(
            virtualAccountId,
            new PreviewPayoutRequest { RecipientId = recipientId, Amount = amount, CreateQuote = true });

        await kira.Platform.SendVerificationCodeAsync(new SendVerificationCodeRequest { Email = operatorEmail });

        return await kira.VirtualAccounts.InitiatePayoutAsync(
            virtualAccountId,
            new InitiatePayoutRequest { RecipientId = recipientId, Amount = amount, QuoteId = preview.QuoteId },
            otpCode: await PromptForOtpAsync(),
            idempotencyKey: remittanceId);
    }
}
```

Passe sempre a sua própria `idempotencyKey`, derivada do identificador da operação. Se você omitir, o SDK gera uma — o que torna a chamada idempotente apenas dentro do processo, e um retry após crash criaria um segundo payout.

Paths são **relativos e sem barra inicial**. Isso não é estilo: o sandbox da Kira é um prefixo de path (`/sandbox`), não um subdomínio, e um `/` inicial faria a requisição escapar do prefixo e atingir produção. O cliente rejeita esses paths.

### Elegibilidade não é um booleano

A Kira avalia o usuário contra todos os produtos ativos e dispara o KYC assim que **ao menos um** deles tiver seus campos completos. Um `201` na criação não significa que a verificação começou.

```csharp
var user = await kira.Users.CreateAsync(request, idempotencyKey: onboardingId);

if (user.VerificationTriggered != true)
{
    logger.LogWarning("KYC não disparou. Faltam: {Fields}", string.Join(", ", user.MissingFields));
}
```

### Casing de enums

A Kira mistura quatro convenções no mesmo payload: `verification_link` (snake), `INSTANT_PAY` (screaming snake), `usa-virtual-accounts` (kebab) e `Full` (pascal). Cada enum declara o seu próprio conversor. **Não adicione um `JsonStringEnumConverter` global** em `KiraJson.Options`: converters da coleção têm precedência sobre o atributo do tipo e uniformizariam tudo, quebrando três das quatro.

### Webhooks

```csharp
var raw = await new StreamReader(Request.Body).ReadToEndAsync(ct);
var bytes = Encoding.UTF8.GetBytes(raw);

if (!KiraWebhookVerifier.IsValid(bytes, Request.Headers["x-signature-sha256"], secret))
{
    return Results.Unauthorized();
}
```

Verifique sobre os **bytes brutos** do corpo. Reserializar o JSON altera espaçamento e ordem de chaves, invalidando o HMAC. Deduplique por `event_id` — a Kira não documenta sua política de retry.

## Credenciais

`ClientId`, `Password` e `ApiKey` são secrets. Ficam no `pass`, grupo `kira/`, e chegam à aplicação por configuração — nunca em código, nunca em `appsettings.json` versionado.

```bash
pass show kira/sandbox-client-id
pass show kira/sandbox-api-key
pass show kira/webhook-secret
```

## Contrato canônico de resposta (Response Envelope)

Este repositório publica **dois pacotes**: `CambioReal.Kira.Client` (o SDK, descrito acima) e
[`CambioReal.Contracts`](src/CambioReal.Contracts/README.md) — a referência do contrato de
resposta canônico da plataforma (`Envelope<T>`, `ProblemDetail` — RFC 9457, `Warning`,
`ResponseMetadata`, `PagedMetadata`), **sem nenhuma dependência de Kira ou de infraestrutura**.
RFC completa: `_pipeline/rfc-kira-sdk-canonical-response-envelope.md` no vault (auditoria de
`cambio-real-v3`, alternativas consideradas, plano de migração).

Inicialmente estes tipos foram implementados dentro do namespace do Kira Client
(`CambioReal.Kira.Contracts`) — **movidos para um pacote próprio em seguida**: um contrato
"canônico" que só existe amarrado a uma integração específica não é reutilizável de verdade.
Qualquer serviço da plataforma sem nenhuma relação com Kira pode referenciar
`CambioReal.Contracts` sozinho, sem herdar `HttpClient` config, `KiraOptions`, handler de auth ou
conversores snake_case da Kira.

**O `KiraClient` continua devolvendo `T` e lançando `KiraApiException` em falha** — o contrato
canônico não muda a superfície pública do SDK. Bibliotecas cliente .NET usam exceções, não
`Result<T>`/`Envelope<T>`, como retorno (convenção estabelecida, Microsoft REST API Guidelines);
forçar `Envelope<T>` aqui seria adotar idioma de servidor HTTP dentro de um cliente, quebrando os
testes existentes e a lógica de retry/single-flight de token já embutida em
`KiraAuthenticationHandler`, sem necessidade real — o gap que motivou este contrato (achado da
auditoria) é inteiramente do lado do *servidor* `cambio-real-v3`, não deste SDK.

O que o `CambioReal.Kira.Client` acrescenta é a peça de integração:
`KiraApiException.ToProblemDetails()` (em `CambioReal.Kira.Contracts`, dentro do SDK — não
confundir com o pacote `CambioReal.Contracts`) traduz qualquer exceção do SDK em um ou mais
`ProblemDetail`, prontos para compor o `Envelope<T>` de saída de quem consumir o kira-sdk dentro
de um serviço HTTP.

```csharp
try
{
    await kira.PayIns.CreateAsync(request);
}
catch (KiraApiException ex)
{
    // Um ProblemDetail por campo quando a Kira devolveu {"details":[{"path","message","code"}]}
    // (confirmado contra o sandbox — ver seção abaixo); um único ProblemDetail caso contrário.
    var envelope = Envelope.Fail<PayInResult>(ex.ToProblemDetails(), "PAYIN_CREATE_FAILED", "Falha ao criar o PayIn.");
    return Results.Json(envelope, EnvelopeJson.Options, statusCode: envelope.Errors[0].Status);
}
```

`Retryable` e `Severity` são computados a partir do `HttpStatusCode` da exceção (429/502/503/504/500
→ `Retryable=true`; `>=500` → `Severity.Critical`) — o chamador não precisa declarar nada.
Serialização usa `EnvelopeJson.Options` (`src/CambioReal.Contracts/Serialization/EnvelopeJson.cs`),
**camelCase**, deliberadamente separado de `KiraJson.Options` (snake_case — o formato de fio *da
Kira*, não o nosso contrato de saída).

**Nota de portabilidade para `cambio-real-v3`**: portar este padrão para um servidor ASP.NET Core
real exige, além de reusar estes tipos: (1) `Envelope<T>` como corpo de toda resposta de
controller/endpoint, não mais DTO nu; (2) um `IExceptionHandler` global — a auditoria confirmou
que **não existe hoje** nenhum handler global de exceção em cambio-real-v3, então falhas
inesperadas não geram envelope nenhum; (3) `ResponseMetadata` populado a partir do `HttpContext`/
`Activity` do OpenTelemetry (trace/span já existem na infra, só não chegam à resposta); (4)
substituir as duas formas de paginação hoje incompatíveis (`CursorPage`/`PagedResult`) por
`PagedMetadata` único, isolado de `Data`.

## Contradições conhecidas na documentação da Kira

Encontradas ao ler a documentação em 2026-07-09. Nenhuma foi resolvida contra o sandbox ainda; até lá, tratar como risco.

| # | Conflito | Onde |
|---|---|---|
| 1 | Path do payout: `/v1/payouts` vs `/v1/virtual-accounts/{id}/payout` | Idempotency vs guia de Payouts |
| 2 | OTP em payout fiat: obrigatório vs opcional (o guia se contradiz internamente) | API Reference vs guia de Payouts |
| 3 | Expiração da quote: 10 min vs 15 min | Quotations vs `initiatePayout` |
| 4 | Rails aceitos: WIRE/SWIFT vs +ACH/INSTANT_PAY. `INSTANT_PAY` não existe na criação de recipient | API Reference vs guia de Payouts |
| 5 | Nomes de eventos de webhook misturam `user.created` e `card_payment`; `payout.*` e `user.verification.failed` são citados mas não catalogados | Webhooks Event Types |
| 6 | Providers têm dois vocabulários: `portage`/`austin_capital_trust` vs `usa-virtual-accounts`/`usa-virtual-accounts-act` | Virtual Accounts vs `createUser` |
| 7 | `/v1/batch-payouts` aparece só na lista de idempotência, sem documentação | Idempotency |
| 8 | Wallets e cashPay são anunciados como produtos, mas não têm endpoints documentados | Product Comparison |

Cada uma delas está anotada no código, no membro correspondente de `KiraPaths` ou do modelo afetado.

### Buracos na documentação

Não existe endpoint de **quotation**: a única forma documentada de obter uma quote é `create_quote: true` no `previewPayout`. Não existe **consulta de payout** — nenhum `GET /payouts/{id}` — então o webhook `transaction_update` é a única forma de acompanhar o status, o que torna webhooks um componente obrigatório da integração, não opcional. Não existe **listar ou remover webhook**, só registrar. E `POST /verification/send`, `/v1/batch-payouts` e `/v1/users/{id}/wallets` são citados de passagem sem especificação — o primeiro está implementado com path inferido; os outros dois, não.

Além disso: `expires_in` é 3600 s e **não há refresh token**. O host é `api.balampay.com` (Kira é a marca, Balam Pay é a infraestrutura).

## Confirmado contra o sandbox (2026-07-13)

Primeira sondagem real contra `https://api.balampay.com/sandbox/`, com credenciais de teste. Feita por fora do SDK (`curl` direto) porque o próprio `POST /auth` já não bate com o contrato assumido — o achado #1 abaixo bloqueia qualquer chamada do `KiraClient` até ser corrigido. Um usuário de teste real foi criado no processo (`id 2e0b058e-c46f-4741-8ffb-73bd3ff5e87a`, sandbox); não há endpoint de exclusão, então ele persiste.

**Isto substitui a premissa "3600 s" acima quanto a `expires_in`**: o valor real observado foi **86400 s (24 h)**. O código já lê `expires_in` dinamicamente, então não há bug de comportamento — só o comentário estava errado.

### Não existe um envelope único — cada família de endpoint tem o seu

Sucesso, confirmados 4 formatos distintos:

| Endpoint | Formato |
|---|---|
| `POST /auth` | `{"message": "...", "data": {access_token, expires_in, token_type}}` |
| `GET /v1/countries` | `{"count": N, "data": [...]}` |
| `GET /v1/users`, `GET /v1/virtual-accounts` | `{"data": [...], "pagination": {total, limit, offset, has_more}}` — paginação por **offset**, não por página |
| `POST /v1/users` | plano, sem envelope |

Erro, confirmados 6 formatos distintos:

| Formato | Onde observado |
|---|---|
| `{"error": {"code", "message", "details"}}` | `GET /v1/recipients` (validação, usuário não encontrado) |
| `{"error": "string", "details": [{"path","message","code"}]}` | `GET /v1/users/{id}` com ID mal formado, `POST /v1/payins` |
| `{"code", "message"}` plano | `GET /v1/users/{id}` não encontrado (404 real) |
| `{"code", "message"}` plano, para rota não casada | Paths inexistentes — mas também aparece quando falta o Bearer em endpoint que exige (ver webhooks) |
| `{"code", "error": "string", "details": [{"message"}]}` (sem `path`/`code` por item) | `POST /v1/payment-link` |
| `{"data": [{"loc","msg","type"}], "message": "ERROR-VXXX: ..."}` (estilo FastAPI/Pydantic) | `POST /webhooks/register` |

Era coberto pelos formatos 2 e 3 apenas (campo `code`/`error_code`/`error` como string na raiz); **corrigido em 2026-07-14** para também cobrir o formato 1 (`error` como objeto — extrai `.code`, com fallback para `.message`) e o formato 6 (fallback final para `message` na raiz).

### Por recurso — estado da correção

Cada item abaixo foi corrigido e reverificado contra o sandbox real (não só contra fakes) antes de avançar para o próximo, conforme decidido em 2026-07-13. ✅ = corrigido e confirmado; ⏳ = achado, correção pendente.

- ✅ **Auth** (`AuthTokenResponse`) — corrigido com `AuthTokenEnvelope` desempacotando `data`. Bloqueava o cliente inteiro; confirmado que `POST /auth` funciona ponta a ponta agora.
- ✅ **`KiraCountry`** — `Code` mapeado explicitamente para `alpha3` via `[JsonPropertyName]`; `ListCountriesAsync` desempacota o envelope `{count, data}` via `KiraCountriesEnvelope` interno.
- ✅ **`KiraPage<T>`** — reestruturado para `{Data, Pagination}`, com `KiraPagination{Total, Limit, Offset, HasMore}` batendo com a resposta real. **Não confirmado**: se o parâmetro de query `page` que `Users.ListAsync`/`VirtualAccounts.ListAsync` enviam é aceito pela API tal como está, ou se ela espera `offset` — só o formato da *resposta* foi sondado.
- ✅ **`KiraUser` / `CreateUserResponse`** — `EligibleProducts` agora é `IReadOnlyList<KiraEligibleProduct>` (objetos com `ProductId` como `string`, não enum, para tolerar produtos futuros não documentados — já apareceu um: `usa-virtual-accounts-zenus`, adicionado ao enum `KiraProduct` só como referência). Descoberta adicional ao corrigir: `MissingFields` tem **duas formas diferentes** dependendo do endpoint — ausente em `POST /v1/users` (fica dentro de cada `KiraEligibleProduct.MissingFields`), mas um **dicionário** `{"produto": [...], "general": [...]}` em `GET /v1/users/{id}` — não a lista simples assumida antes. Corrigido como `IReadOnlyDictionary<string, IReadOnlyList<string>>?`.
- ✅ **`CreateRecipientRequest`/`KiraRecipient`** — reescritos e verificados com uma criação real (rail `WALLET`, ponta a ponta: criar, buscar por id, listar por usuário). Dados de conta agora aninhados em `Account` (`RecipientAccount`, discriminado por `AccountType`), com nome/sobrenome/razão social na raiz. Resposta usa `recipient_id`/`account_details`/`created_ts`/`updated_ts` (formato de timestamp não padrão, corrigido via `KiraTimestampConverter`), e a listagem usa um terceiro formato de envelope (`{"recipients","total"}`). Confirmado só para `WALLET`; `SWIFT` teve apenas os campos obrigatórios sondados (`account_number`, `swift_code`, `bank_name`, `bank_address`); os demais ~18 rails permanecem por inferência.
- ✅ **`CreateVirtualAccountRequest`** — `Type` (`VirtualAccountType`: `US_ACH`/`MX_SPEI`/`EU_SEPA`, substitui a string livre `US_BANK`) e `Destination` (`VirtualAccountDestination` com `currency`/`network`/`address`) confirmados obrigatórios via erro de validação. Não foi possível completar um `201`: o usuário de teste não reúne KYC suficiente ("Customer not found... A customer is required for US_ACH accounts") — a correção de schema está confirmada, o fluxo feliz completo não.
- ✅ **`CreatePayInRequest`** — `Currency`, `CallbackUrl` e `Settlement` (`PayInSettlementInput`, reaproveitando o mesmo formato de `RecipientAccount`) adicionados; discriminador real é `Type`, não `Method`. Verificado até a camada de negócio (`INVALID_BANK_CODE`, não mais erro de schema) para o rail `PSE`. Achado não resolvido: `type=SPEI` foi rejeitado pela validação (`Expected 'PSE', received 'SPEI'`) — o rail documentado pode não estar habilitado neste client/sandbox.
- ✅ **`CreatePaymentLinkRequest`/`KiraPaymentLink`** — criação real bem-sucedida (`country_code=BR`/`currency=BRL`). `ClientUuid` (o mesmo `KiraOptions.ClientId`, não um `user_id`), `Reference` (antes opcional) e `CountryCode` (ISO **alpha-2**, não alpha-3) são obrigatórios. A doc dizia "sempre USD" — `BRL` funcionou; `country_code=US` tem uma restrição de negócio adicional não resolvida (`acct_type must be USD for US recipients`). Resposta usa `txn_uuid`/`payment_link` (não `id`/`url`); `amount`/`currency`/`expires_at` não vieram de volta na criação.
- ✅ **`RegisterWebhookRequest`/`KiraWebhookRegistration`** — registro real bem-sucedido. Confirmado que o endpoint **exige Bearer** (a doc e o código anteriores diziam o contrário); `ClientUuid` e `WebhookUrl` (não `Url`) são obrigatórios. Resposta de sucesso é só `{"message": "..."}`, sem identificador.
- **`GET /banks`** (path confirmado correto, `v1/banks` dá `400`) — instável no sandbox: devolve consistentemente `HTTP 200` com corpo literal de erro (`"error code: 522"` ou variações) em vez de JSON, mesmo após as correções acima. Confirmado problema do lado da Kira, não do cliente — sem ação possível deste lado além de tratar erros de parse com mais grace (não feito ainda).
- **`POST /v1/payins/fees`** — não existe em nenhuma variação testada (`v1/payins/fees`, `v1/pay-ins/fees`, `v1/payin/fees`, GET ou POST): sempre `403` "rota não corresponde". O endpoint documentado de cálculo de taxas pode ter um path totalmente diferente do que a doc descreve, ou não existir no sandbox atual.

Um usuário de teste real (`id 2e0b058e-c46f-4741-8ffb-73bd3ff5e87a`) e vários recipients/payment-links/webhooks de teste ficaram criados no sandbox no processo — não há endpoints de exclusão, então persistem. Reutilizar esse usuário em vez de criar novos ao continuar a sondagem (ex.: os rails ainda não confirmados de `RecipientAccount`, ou um usuário com KYC completo para testar `CreateVirtualAccountRequest` até `201`).

## Build

```bash
dotnet build Kira.slnx -c Release
dotnet test  Kira.slnx -c Release
```

Convenções herdadas do `cambio-real-v3`: `net10.0` central (os `.csproj` não declaram `TargetFramework`), Central Package Management com transitive pinning, `TreatWarningsAsErrors`, e — por ADR-001 — Shouldly em vez de FluentAssertions.
