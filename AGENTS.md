# AGENTS.md

Guidance for AI coding agents (Claude Code, and any other AGENTS.md-aware tool) working in this repository.

## What this is

`CambioReal.Kira.Client` is a typed .NET client for the Kira Financial AI API (`api.balampay.com`) — receiving, custody, and payout of international transactions. Primary consumer: [`cambioreal/cambio-real-v3`](https://github.com/cambioreal/cambio-real-v3). Target `net10.0`, SDK pinned to `10.0.301` (`global.json`).

All 27 documented Kira endpoints are implemented and typed. Types were derived from prose documentation that has known internal contradictions (see README.md for the full list) — **and a 2026-07-13/14 sandbox probe found the models diverged from the real API in most resources, then fixed and verified every one of them live against the sandbox** (not just against test fakes). See README.md's "Confirmado contra o sandbox (2026-07-13)" section for the full per-resource detail before touching any model. All ten confirmed mismatches (auth envelope, `KiraCountry`, `KiraPage<T>` pagination, error-code extraction, `KiraUser`/`EligibleProducts`/`MissingFields`, `CreateRecipientRequest`/`KiraRecipient`, `CreateVirtualAccountRequest`, `CreatePayInRequest`, `CreatePaymentLinkRequest`/`KiraPaymentLink`, `RegisterWebhookRequest`/`KiraWebhookRegistration`) are fixed as of 2026-07-14. **A 2026-07-15 follow-up closed most of the remaining `RecipientAccount` gap**: see README.md's "Confirmado contra o sandbox (2026-07-15)" section. Of the ~19 non-`WALLET` rails, 9 are now confirmed end-to-end (`SWIFT`, `ACH`, `WIRE`, `INSTANT_PAY`, `SPEI`, `BRL`, `CRC`, `GTQ`, `SVUSD`) — this fixed a real bug (`bank_address` is a string for ACH/INSTANT_PAY but a structured object for SWIFT/WIRE; `document_type`/`document_number` were the wrong wire names, real ones are `doc_type`/`doc_number`) and added previously-unmodeled fields (`Address` on the request root, `DocCountryCode`, `City`, `Type`, `PixKeyType`, `PixKey`). The remaining 10 (`PSE`, `ARS`, `CLP`, `DOP`, `ECUSD`, `PAUSD`, `PEN`, `PEUSD`, `PYG`, `UYU`) have a fully schema-valid payload confirmed but are blocked Kira-side: `bank_code` must match an internal per-country table only discoverable via `GET /banks`, which is broken for every country tested. `CreateVirtualAccountRequest` still never reached a full `201` (the test user lacks KYC — reconfirmed 2026-07-15), and the original prose-doc contradictions (payout paths, OTP, quote expiry — the table above this section) remain untouched. `GET /banks` and `POST /v1/payins/fees` are confirmed Kira-side issues (not fixable client-side), reconfirmed unchanged on 2026-07-15. Kira sandbox credentials live in `pass` (group `kira/`) and Bitwarden (`kira` item in `Infra & Dev Secrets/cambio-real/sandbox-providers`); there's a persistent real test user in the sandbox (`id 2e0b058e-c46f-4741-8ffb-73bd3ff5e87a`) plus several test recipients/payment-links/webhooks created while verifying these fixes — reuse them instead of creating new ones when probing further.

## Commands

```bash
dotnet restore Kira.slnx
dotnet build Kira.slnx -c Release
dotnet test Kira.slnx -c Release
dotnet pack src/CambioReal.Kira.Client/CambioReal.Kira.Client.csproj -c Release -o artifacts
```

Run a single test (xunit filter, works with class or fully-qualified method name):

```bash
dotnet test Kira.slnx --filter "FullyQualifiedName~ResourceCoverageTests.CreateWalletRecipientSerializesNetworkAndToken"
```

`TreatWarningsAsErrors` is on — a build with any warning fails, matching CI (`.github/workflows/ci.yml`). Release publishing (`.github/workflows/release.yml`) fires on `v*` tags, runs the test suite again, and pushes to GitHub Packages under `nuget.pkg.github.com/cambioreal`; the package version comes from the tag, not `Directory.Build.props`.

## Architecture

### Transport layer (`KiraClient`)

`KiraClient` is the generic HTTP layer: `GetAsync<T>`, `PostAsync<TReq,TRes>`, `PatchAsync<TReq,TRes>`, plus a `SendAsync(HttpRequestMessage)` escape hatch. It owns six resource facades constructed in its constructor:

| Facade | Endpoints | Covers |
|---|---|---|
| `Users` | 5 | users, verification, product eligibility |
| `Recipients` | 3 | payout recipients |
| `VirtualAccounts` | 11 | virtual accounts, deposits, payouts, liquidation addresses |
| `PayIns` | 3 | PSE/SPEI collection |
| `PaymentLinks` | 1 | payment links |
| `Platform` | 4 | webhooks, countries, banks, OTP issuance |

Resource classes live in `src/CambioReal.Kira.Client/Resources/` but don't map 1:1 to files — `UsersResource.cs` also contains `RecipientsResource`, and `PlatformResource.cs` also contains `PayInsResource` and `PaymentLinksResource`. Check `KiraClient.cs`'s constructor, not file names, to find where a facade lives.

Every path passed to `KiraClient` must be **relative with no leading slash**. `KiraClient` throws `ArgumentException` otherwise — this isn't style, it's correctness: the sandbox is a path prefix (`/sandbox`), not a subdomain, and `Uri` resolution drops the last segment of the base address when a relative path starts with `/`, silently routing sandbox test traffic to production. Paths are centralized in `KiraPaths.cs`, each annotated with the doc contradiction it resolves (see README's "Contradições conhecidas" table) so a fix is a one-line change there.

### Auth (`src/CambioReal.Kira.Client/Auth/`)

- `KiraTokenProvider` — singleton that caches the JWT and single-flights renewal via a `SemaphoreSlim` so a burst of requests after expiry produces one reauth, not N. The Kira token lives 3600s with **no refresh token**; renewal means repeating `POST /auth`.
- `KiraAuthenticationHandler` — a `DelegatingHandler` that injects `x-api-key` on every request and `Authorization: Bearer` where required, retrying exactly once on a 401 (clones the request via `HttpRequestMessageExtensions.CloneAsync` before the first send, since `Content` is disposed after sending). A second 401 propagates up as `KiraAuthenticationException` — wrong credentials aren't fixed by retrying.
- `KiraServiceCollectionExtensions.AddKiraClient(...)` registers two named `HttpClient`s: one for `/auth` itself (no auth handler attached, to avoid recursion) and one for the API (`KiraAuthenticationHandler` attached). Both share `KiraOptions.ResolveBaseAddress()` / `Timeout`.

### Serialization (`src/CambioReal.Kira.Client/Serialization/`)

`KiraJson.Options` uses `SnakeCaseLower` for property/dictionary-key naming and reads numbers from strings (`NumberHandling = AllowReadingFromString`) because Kira returns money as strings (`"1000.00"`).

**Do not add a global `JsonStringEnumConverter` to `KiraJson.Options`.** The API mixes four enum casing conventions in the same payload — snake_case (`verification_link`), SCREAMING_SNAKE (`INSTANT_PAY`), kebab-case (`usa-virtual-accounts`), PascalCase (`Full`) — so each enum declares its own `[JsonConverter]` from `EnumConverters.cs` (`SnakeCaseLowerEnumConverter<T>`, `UpperSnakeCaseEnumConverter<T>`, `KebabCaseLowerEnumConverter<T>`). A converter added to the global `Converters` collection takes precedence over a type's own attribute and would uniformize all four — breaking three of them.

### Models (`src/CambioReal.Kira.Client/Models/`)

Split by domain: `Common`, `Enums`, `PayIns`, `Payouts`, `Recipients`, `Reference`, `Users`, `VirtualAccounts`. Response types carry `[JsonExtensionData] AdditionalData` so unmodeled fields aren't lost — useful given the docs are known-incomplete.

### Webhooks

`KiraWebhookVerifier.IsValid(...)` checks the `x-signature-sha256` HMAC-SHA256 header in constant time (`CryptographicOperations.FixedTimeEquals`), accepting both hex and base64 digests with an optional `sha256=` prefix (the API docs show only a Node example and don't specify encoding). **Verify against the raw request bytes** — re-serializing JSON changes whitespace/key order and invalidates the HMAC. There's no documented retry policy, so dedupe by `event_id` on the consumer side.

### Errors

`KiraApiException` (base) → `KiraAuthenticationException` (401 after retry) / `KiraIdempotencyConflictException` (409 — idempotency key reused with a different body; the fix is to reuse the original body or mint a new key, not to retry). `KiraClient.ThrowIfUnsuccessfulAsync` maps status codes to these; `TryExtractErrorCode` (fixed 2026-07-14) covers 3 of the ≥6 error-body shapes confirmed against the sandbox — see README.

### Contracts — two packages, not one

This repo publishes **two NuGet packages** from the same solution (`Kira.slnx`): `CambioReal.Kira.Client` (the SDK itself) and `CambioReal.Contracts` (`src/CambioReal.Contracts/`, zero dependencies) — the canonical platform response contract (`Envelope<T>`, `ProblemDetail` per RFC 9457, `Warning`, `ResponseMetadata`, `PagedMetadata`). Added 2026-07-14, initially inside the Kira Client's own namespace and **moved out same-day** on the reasoning that a "canonical" contract tied to one integration's package isn't actually reusable — see README "Contrato canônico de resposta", `src/CambioReal.Contracts/README.md`, and `_pipeline/rfc-kira-sdk-canonical-response-envelope.md` in the vault for the full RFC (includes the `cambio-real-v3` audit that motivated this — it has no equivalent contract, and what little exists there, `ResultProblemDetailsMapper` in `CambioReal.BuildingBlocks.Web`, is hard-coupled to ASP.NET Core via `FrameworkReference` and never published as a package).

**Does not change `KiraClient`'s public surface** — it still returns `T` and throws `KiraApiException`; `CambioReal.Contracts` is the reference implementation of the contract types for whoever consumes any service inside an HTTP boundary (starting with `cambio-real-v3`), and `CambioReal.Kira.Client` references it (never the other way — `CambioReal.Contracts` has zero dependency on Kira or on this SDK). The Kira-specific integration point lives in the SDK: `KiraApiException.ToProblemDetails()` (namespace `CambioReal.Kira.Contracts`, folder `src/CambioReal.Kira.Client/Contracts/` — don't confuse this folder/namespace with the `CambioReal.Contracts` package) — one `ProblemDetail` per field when Kira returned a `details[]` array, one otherwise. Serializes via `CambioReal.Contracts.Serialization.EnvelopeJson.Options` (camelCase) — do not confuse with `KiraJson.Options` (snake_case, Kira's own wire format, stays in the SDK). CI/release workflows (`.github/workflows/`) pack and push **both** packages.

### Idempotency / OTP / request modifiers

`KiraRequestContext` (`Http/KiraRequestContext.cs`) carries per-call modifiers: `IdempotencyKey` (required on creation POSTs — resend the same key with an identical body to get the stored response back, `409` on a differing body), `ValidationCode` (6-digit OTP for fiat payouts, header is literally `x-validation-header`), and `SkipBearerAuthentication` (escape hatch — confirmed 2026-07-14 that no real endpoint needs it; `POST /webhooks/register` was assumed to but actually requires Bearer like everything else).

## Non-obvious domain facts

- **Eligibility is not a boolean.** Kira evaluates a user against every active product and triggers KYC once *any* product's required fields are complete. A `201` from user creation does not mean verification started — check `VerificationTriggered` / `MissingFields` on the response.
- **No quotation endpoint exists.** The only way to get a quote is `create_quote: true` on `previewPayout`. **No payout-status endpoint exists either** (no `GET /payouts/{id}`) — the `transaction_update` webhook is the only way to track payout status, making webhooks a mandatory integration component, not optional.
- Known documentation contradictions (path disagreements, OTP requiredness, quote expiry window, accepted payment rails, webhook event naming, provider vocabulary, undocumented `/v1/batch-payouts`, undocumented wallet/cashPay products) are enumerated in the README and annotated at the corresponding `KiraPaths` member or model.

## Dependency / license constraints (inherited from `cambio-real-v3`, ADR-001)

- **No MediatR, no AutoMapper** (commercial licensing trajectory).
- **No FluentAssertions v8+** — use **Shouldly** (already the test dependency).
- `Directory.Packages.props` pins **floor** versions, not latest-available. Don't bump a package to "the newest version" reflexively — a higher floor than what `cambio-real-v3`'s own `Directory.Packages.props` pins (with `CentralPackageTransitivePinningEnabled`) breaks that consumer with `NU1109` (package downgrade). Confirm against the downstream repo before raising a floor.
- `TargetFramework` is centralized in `Directory.Build.props`; individual `.csproj` files must not declare it.

## Testing conventions

- xunit + **Shouldly** (not FluentAssertions — see above).
- Fakes live in `tests/CambioReal.Kira.Client.Tests/Fakes/`: `RecordingHttpMessageHandler` (captures sent requests), `StubTokenProvider`, `MutableTimeProvider`, and `TestClient` (helper that wires a `KiraClient` over a recording transport with a fixed bearer token — `TestClient.Create(...)` for scripted responses, `TestClient.CreateOk(json)` for a single 200).
- `ResourceTests.cs` and `ResourceCoverageTests.cs` together aim for one test per resource method; when adding a new resource method, add its test to whichever file doesn't already cover that resource area rather than assuming one file owns a resource.
- Assert on `RequestUri.AbsoluteUri` (not `.ToString()`) when checking query strings — `ToString()` decodes spaces back out of `%20` and would mask encoding bugs.

## Secrets

`ClientId`, `Password`, `ApiKey` are secrets sourced from `pass`, group `kira/` (`pass show kira/sandbox-client-id`, etc.), injected via configuration — never hardcoded, never committed in `appsettings.json`.
