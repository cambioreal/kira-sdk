# CambioReal.Kira.Client

Cliente .NET tipado para a [Kira Financial AI API](https://kira-financial-ai.readme.io) — recebimento, custódia e pagamento de transações internacionais.

Consumidor primário: [`cambioreal/cambio-real-v3`](https://github.com/cambioreal/cambio-real-v3). Alvo `net10.0`, SDK fixada em `10.0.301`.

## Estado

Esta é a **camada de transporte**. Ela está completa e testada:

- autenticação JWT com cache, single-flight e renovação automática em 401;
- `x-api-key` em toda requisição, `Authorization: Bearer` onde a API exige;
- idempotência (`idempotency-key`) e OTP (`x-validation-header`);
- tradução de erros HTTP em exceções tipadas;
- verificação de assinatura HMAC de webhooks, em tempo constante.

Os **recursos tipados** (`users`, `virtual-accounts`, `payouts`, `payins`, `recipients`) **ainda não estão modelados**. A Kira não publica um OpenAPI e a documentação descreve os payloads em prosa, com contradições conhecidas (ver abaixo). Modelar esses contratos a partir da prosa produziria tipos que compilam e falham em produção. Use `GetAsync`/`PostAsync` com seus próprios contratos até que o probe contra o sandbox confirme os schemas.

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
public sealed class OnboardingService(KiraClient kira)
{
    public Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct) =>
        kira.PostAsync<CreateUserRequest, CreateUserResponse>(
            "v1/users",
            request,
            KiraRequestContext.WithNewIdempotencyKey(),
            ct);
}
```

Paths são **relativos e sem barra inicial**. Isso não é estilo: o sandbox da Kira é um prefixo de path (`/sandbox`), não um subdomínio, e um `/` inicial faria a requisição escapar do prefixo e atingir produção. O cliente rejeita esses paths.

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

Além disso: `expires_in` é 3600 s e **não há refresh token**. O host é `api.balampay.com` (Kira é a marca, Balam Pay é a infraestrutura).

## Build

```bash
dotnet build Kira.slnx -c Release
dotnet test  Kira.slnx -c Release
```

Convenções herdadas do `cambio-real-v3`: `net10.0` central (os `.csproj` não declaram `TargetFramework`), Central Package Management com transitive pinning, `TreatWarningsAsErrors`, e — por ADR-001 — Shouldly em vez de FluentAssertions.
