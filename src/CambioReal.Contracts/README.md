# CambioReal.Contracts

Contrato canônico de resposta HTTP da plataforma CambioReal — um único formato para sucesso e
erro, independente de módulo, domínio ou serviço.

Sem dependências de infraestrutura, framework web, ou de qualquer integração específica (Kira ou
outra). Pense neste pacote como o "vocabulário" compartilhado; cada serviço/SDK que o referencia
é responsável por preenchê-lo e traduzir suas próprias falhas para ele.

## Tipos

- `Envelope<T>` — `Success`, `Code`, `Message`, `Data` (nunca `{}`/`[]`/`""` para ausência —
  sempre `null`), `Errors`, `Warnings`, `Metadata`, `Links` opcionais (HATEOAS). Construído via a
  fábrica não-genérica `Envelope.Ok(...)`/`Envelope.Fail(...)` (permite inferência do argumento
  de tipo — `Envelope<T>` em si não tem membros estáticos, por `CA1000`).
- `ProblemDetail` — erro seguindo RFC 9457 (Problem Details for HTTP APIs): `Type`, `Status`,
  `Code`, `Title`, `Detail`, `Field`, `Target`, `DocumentationUrl`, `Retryable`, `Severity`.
- `Warning` — aviso não bloqueante: `Code`, `Message`, `Field`, `DocumentationUrl`.
- `ResponseMetadata` — `Timestamp`, `RequestId`, `CorrelationId`, `TraceId`, `SpanId`,
  `DurationMs`, `ApiVersion`, `Service`, `Environment`, `Region`, `TenantId`,
  `AuthenticatedUser`, `Locale`, `Pagination`, `Extensions` (dicionário aberto para campos
  técnicos ad-hoc: cache, rate limit, idempotency key, retry count, ...).
- `PagedMetadata` — `Page`, `PageSize`, `TotalItems`, `TotalPages`, `HasNext`, `HasPrevious`,
  sempre isolado de `Data`, nunca misturado ao payload.

## Serialização

`CambioReal.Contracts.Serialization.EnvelopeJson.Options` — camelCase, deliberadamente separado
do formato de fio de qualquer integração específica (ex.: o `KiraJson` do
[`kira-sdk`](https://github.com/cambioreal/kira-sdk) é snake_case — o formato *da Kira*, não o
contrato de saída *da plataforma*).

## Uso

```csharp
// Caminho feliz
var envelope = Envelope.Ok(customer, "CUSTOMER_CREATED", "Cliente criado com sucesso.");

// Traduzindo uma falha de uma dependência (ex.: kira-sdk) para o contrato de saída
try
{
    await kira.Users.CreateAsync(request);
}
catch (KiraApiException ex)
{
    var envelope = Envelope.Fail<CustomerDto>(
        ex.ToProblemDetails(), "CUSTOMER_CREATE_FAILED", "Falha ao criar o cliente.");
    return Results.Json(envelope, EnvelopeJson.Options, statusCode: envelope.Errors[0].Status);
}
```

`ex.ToProblemDetails()` é uma extensão específica do
[`kira-sdk`](https://github.com/cambioreal/kira-sdk) (`CambioReal.Kira.Contracts`), não deste
pacote — a demonstração de como uma integração concreta se traduz para o contrato canônico.

## Origem

Extraído do [`kira-sdk`](https://github.com/cambioreal/kira-sdk) em 2026-07-14 como a
implementação de referência do contrato pedido para toda a plataforma — ver
`_pipeline/rfc-kira-sdk-canonical-response-envelope.md` no vault para o RFC completo (auditoria
de `cambio-real-v3`, alternativas consideradas, plano de migração). Extraído para um pacote
próprio, sem dependência de Kira, especificamente para que qualquer serviço da plataforma possa
adotá-lo sem precisar referenciar um SDK de integração de terceiros só para pegar os tipos.
