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

> **Nenhum endpoint foi exercitado contra o sandbox ainda.** Os tipos foram derivados da documentação em prosa, que tem contradições conhecidas (ver abaixo). Respostas trazem `AdditionalData` (via `[JsonExtensionData]`) para que campos não modelados não se percam, e os paths estão centralizados em `KiraPaths` para que cada correção seja de uma linha.

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

## Build

```bash
dotnet build Kira.slnx -c Release
dotnet test  Kira.slnx -c Release
```

Convenções herdadas do `cambio-real-v3`: `net10.0` central (os `.csproj` não declaram `TargetFramework`), Central Package Management com transitive pinning, `TreatWarningsAsErrors`, e — por ADR-001 — Shouldly em vez de FluentAssertions.
