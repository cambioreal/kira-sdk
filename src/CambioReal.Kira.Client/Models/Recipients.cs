using System.Text.Json.Serialization;
using CambioReal.Kira.Serialization;

namespace CambioReal.Kira.Models;

/// <summary>
/// Corpo de <c>POST /v1/recipients</c>. Exige chave de idempotência.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14 para o rail <see cref="Models.AccountType.Wallet"/>
/// (criado e verificado ponta a ponta): os dados bancários/wallet vão dentro de
/// <see cref="Account"/>, não soltos no corpo — o request antigo (campos flat, sem <c>account</c>)
/// sempre falhava com <c>400 account: Required</c>. Nome e sobrenome (ou razão social) ficam na
/// raiz, não em um único <c>account_holder_name</c>.
/// <para>
/// Sondagem ampliada em 2026-07-15: dos ~19 rails além de <see cref="Models.AccountType.Wallet"/>,
/// 9 foram criados e verificados ponta a ponta (POST 201 + GET de volta) via <c>KiraClient</c> real
/// contra o sandbox — <see cref="Models.AccountType.Swift"/>, <see cref="Models.AccountType.Ach"/>,
/// <see cref="Models.AccountType.Wire"/>, <see cref="Models.AccountType.InstantPay"/>,
/// <see cref="Models.AccountType.Spei"/>, <see cref="Models.AccountType.Brl"/>,
/// <see cref="Models.AccountType.Crc"/>, <see cref="Models.AccountType.Gtq"/> e
/// <see cref="Models.AccountType.Svusd"/>. Os 10 restantes (<see cref="Models.AccountType.Pse"/>,
/// <see cref="Models.AccountType.Ars"/>, <see cref="Models.AccountType.Clp"/>,
/// <see cref="Models.AccountType.Dop"/>, <see cref="Models.AccountType.Ecusd"/>,
/// <see cref="Models.AccountType.Pausd"/>, <see cref="Models.AccountType.Pen"/>,
/// <see cref="Models.AccountType.Peusd"/>, <see cref="Models.AccountType.Pyg"/>,
/// <see cref="Models.AccountType.Uyu"/>) passam na validação de schema com os campos abaixo, mas
/// ficam bloqueados numa camada de validação de negócio posterior: a Kira rejeita qualquer
/// <c>bank_code</c> testado com <c>"Invalid bank code 'X' - bank not found in system for country
/// Y"</c>, e o único jeito documentado de descobrir um código válido — <c>GET /banks</c> — devolve
/// consistentemente um corpo não-JSON (ver README, achado já confirmado independentemente desta
/// sondagem). Uma varredura de <c>bank_code</c> de 2 a 4 dígitos (0–30, com e sem zero à esquerda)
/// contra <see cref="Models.AccountType.Clp"/> não encontrou nenhum código aceito — não é um
/// intervalo pequeno e sequencial, é uma tabela real inacessível sem o endpoint funcionando.
/// Bloqueio do lado Kira, não corrigível neste cliente.
/// </para>
/// <para>
/// Campos desconhecidos devolvidos pela API caem em <see cref="KiraResponse.AdditionalData"/>.
/// </para>
/// </remarks>
public sealed record CreateRecipientRequest
{
    /// <summary>Usuário dono do recipient.</summary>
    public required string UserId { get; init; }

    /// <summary>Nome do beneficiário, para pessoa física.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome do beneficiário, para pessoa física.</summary>
    public string? LastName { get; init; }

    /// <summary>Razão social do beneficiário, para pessoa jurídica.</summary>
    public string? CompanyName { get; init; }

    /// <summary>E-mail do beneficiário.</summary>
    public string? Email { get; init; }

    /// <summary>Telefone do beneficiário.</summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Endereço do beneficiário. Confirmado contra o sandbox em 2026-07-15: obrigatório e sempre em
    /// forma de objeto estruturado (<see cref="KiraAddress"/>) para <see cref="Models.AccountType.Ach"/>,
    /// <see cref="Models.AccountType.Wire"/>, <see cref="Models.AccountType.Swift"/> e
    /// <see cref="Models.AccountType.InstantPay"/> — a mensagem de erro da Kira cita esses quatro
    /// rails nominalmente (<c>"address must be a structured object with street_name, city, state,
    /// postal_code, country for ACH/WIRE/SWIFT accounts"</c>/<c>"...for INSTANT_PAY accounts"</c>).
    /// Uma string solta (o formato usado por <see cref="RecipientAccount.BankAddress"/> em alguns
    /// desses mesmos rails) é rejeitada aqui com <c>invalid_union</c>.
    /// <para>
    /// Tipado como <see cref="object"/>, não <see cref="KiraAddress"/>, só porque
    /// <see cref="RecipientAccount.BankAddress"/> precisa da mesma flexibilidade (string <b>ou</b>
    /// objeto, dependendo do rail) e o serializador não permite duas propriedades C# mapeando para a
    /// mesma chave JSON — ver a nota em <see cref="RecipientAccount.BankAddress"/>. Atribua uma
    /// instância de <see cref="KiraAddress"/> aqui; nenhum rail confirmado aceita string neste campo
    /// específico (raiz), só em <see cref="RecipientAccount.BankAddress"/>.
    /// </para>
    /// </summary>
    public object? Address { get; init; }

    /// <summary>Dados bancários/wallet. O discriminador é <see cref="RecipientAccount.AccountType"/>.</summary>
    public required RecipientAccount Account { get; init; }
}

/// <summary>
/// Endereço estruturado usado tanto na raiz de <see cref="CreateRecipientRequest.Address"/> quanto
/// em <see cref="RecipientAccount.BankAddress"/>, quando o rail exige a forma de objeto.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-15 via erro de validação
/// (<c>account.bank_address.street_name: Required</c>, para <see cref="Models.AccountType.Swift"/>):
/// <see cref="StreetName"/> é obrigatório. Os demais campos estiveram presentes em todo payload que
/// resultou em <c>201</c>, mas nenhum deles foi isolado individualmente — podem ser opcionais.
/// </remarks>
public sealed record KiraAddress
{
    /// <summary>Logradouro. Único campo confirmado obrigatório.</summary>
    public required string StreetName { get; init; }

    /// <summary>Cidade.</summary>
    public string? City { get; init; }

    /// <summary>Estado/província.</summary>
    public string? State { get; init; }

    /// <summary>CEP/código postal.</summary>
    public string? PostalCode { get; init; }

    /// <summary>País, código de duas letras (ex.: <c>US</c>, <c>DE</c>).</summary>
    public string? Country { get; init; }
}

/// <summary>
/// Dados de conta de um recipient — o payload varia por <see cref="AccountType"/>, mas a Kira
/// não publica o schema por rail. Preencha o que o rail exige.
/// </summary>
/// <remarks>
/// Sondagem ampliada contra o sandbox em 2026-07-15 (iterando sobre o erro de validação real da
/// Kira — uma resposta no estilo Zod <c>safeParse</c>, com <c>path</c>/<c>message</c>/<c>code</c>
/// por campo). Payload mínimo confirmado por rail:
/// <list type="bullet">
/// <item><description><b>SWIFT</b> — 201 confirmado. <see cref="AccountNumber"/>, <see cref="SwiftCode"/>,
/// <see cref="BankName"/>, <see cref="BankAddress"/> como <see cref="KiraAddress"/> (objeto — não
/// string) + <see cref="CreateRecipientRequest.Address"/> (objeto) na raiz.</description></item>
/// <item><description><b>ACH</b> / <b>WIRE</b> — 201 confirmado. <see cref="AccountNumber"/>,
/// <see cref="RoutingNumber"/>, <see cref="BankName"/> + <see cref="CreateRecipientRequest.Address"/>
/// (objeto) na raiz. <see cref="BankAddress"/> é <see cref="string"/> para ACH; para WIRE é
/// <see cref="KiraAddress"/> (objeto), como SWIFT.</description></item>
/// <item><description><b>INSTANT_PAY</b> — 201 confirmado, apesar da contradição de documentação
/// registrada no <c>enum</c> (a doc do <c>createRecipient</c> não lista INSTANT_PAY como aceito —
/// na prática aceita). Mesmo payload de ACH: <see cref="AccountNumber"/>, <see cref="RoutingNumber"/>,
/// <see cref="BankName"/>, <see cref="BankAddress"/> string, + endereço-objeto na raiz.</description></item>
/// <item><description><b>SPEI</b> — 201 confirmado. Só <see cref="Clabe"/> + <see cref="BankName"/>;
/// ao contrário dos demais rails LATAM, <see cref="DocType"/>/<see cref="DocNumber"/> não são
/// exigidos (testado sem eles).</description></item>
/// <item><description><b>BRL</b> — 201 confirmado. <see cref="AccountNumber"/>, <see cref="PixKeyType"/>
/// (<c>code_cpf</c> confirmado aceito; a Kira também lista <c>code_cnpj</c>/<c>email</c>/
/// <c>random_key</c>/<c>phone_number</c> no erro de enum), <see cref="PixKey"/>, <see cref="City"/>,
/// <see cref="DocType"/> (<c>cpf</c>), <see cref="DocNumber"/>, <see cref="DocCountryCode"/> = <c>BR</c>.</description></item>
/// <item><description><b>CRC</b> — 201 confirmado. <see cref="AccountNumber"/> (exatamente 22 dígitos),
/// <see cref="BankName"/>, <see cref="DocType"/> ∈ {<c>ci</c>, <c>cr</c>, <c>cj</c>},
/// <see cref="DocNumber"/>, <see cref="DocCountryCode"/> = <c>CR</c>. Sem <see cref="Type"/>.</description></item>
/// <item><description><b>GTQ</b> — 201 confirmado. <see cref="AccountNumber"/>, <see cref="BankCode"/>
/// (<c>001</c> aceito), <see cref="BankName"/>, <see cref="DocType"/> = <c>dpi</c>, <see cref="DocNumber"/>,
/// <see cref="DocCountryCode"/> = <c>GT</c>. Sem <see cref="Type"/>/<see cref="City"/>.</description></item>
/// <item><description><b>SVUSD</b> — 201 confirmado. <see cref="AccountNumber"/>, <see cref="BankCode"/>
/// (<c>001</c> aceito), <see cref="BankName"/>, <see cref="DocType"/> = <c>dui</c>, <see cref="DocNumber"/>,
/// <see cref="DocCountryCode"/> = <c>SV</c>. Sem <see cref="Type"/>/<see cref="City"/>.</description></item>
/// <item><description><b>PSE, ARS, CLP, DOP, ECUSD, PAUSD, PEN, PEUSD, PYG, UYU</b> — <b>bloqueados
/// do lado Kira</b>, não neste cliente. O schema completo (abaixo) passa na validação; o único erro
/// restante em todos os 10 é <c>"Invalid bank code 'X' - bank not found in system for country Y"</c>.
/// O único jeito documentado de descobrir um <see cref="BankCode"/> válido é <c>GET /banks</c>, que
/// devolve consistentemente corpo não-JSON (achado já confirmado independentemente — ver README).
/// Uma varredura de <see cref="BankCode"/> de 2–4 dígitos (0–30, com/sem zero à esquerda) contra CLP
/// não encontrou nenhum código aceito: não é uma faixa pequena e sequencial, é uma tabela real
/// inacessível. Schema confirmado válido (falta só um <see cref="BankCode"/> real) para cada um:
/// <list type="bullet">
/// <item><description>PSE (CO): <see cref="Type"/> = <c>checking</c>, <see cref="DocType"/> ∈
/// {<c>nuip</c>, <c>passport</c>, <c>nit</c>}, <see cref="DocCountryCode"/> = <c>CO</c>.</description></item>
/// <item><description>ARS (AR): <see cref="Type"/> = <c>checking</c>, <see cref="DocType"/> ∈
/// {<c>cuil</c>, <c>cuit</c>}, <see cref="DocNumber"/> com exatamente 11 dígitos,
/// <see cref="BankCode"/> com no máximo 4 dígitos, <see cref="DocCountryCode"/> = <c>AR</c>.</description></item>
/// <item><description>CLP (CL): <see cref="Type"/> ∈ {<c>Cuenta corriente</c>, <c>Cuenta de ahorros</c>,
/// <c>Cuenta Vista</c>} (strings exatas em espanhol, confirmadas via erro de enum),
/// <see cref="DocType"/> = <c>RUT</c> aceito, <see cref="DocCountryCode"/> = <c>CL</c>.</description></item>
/// <item><description>DOP (DO): <see cref="Type"/> = <c>checking</c>, <see cref="DocType"/> ∈
/// {<c>ce</c>, <c>passport</c>, <c>rn</c>}, <see cref="DocCountryCode"/> = <c>DO</c>.</description></item>
/// <item><description>ECUSD (EC): <see cref="Type"/> = <c>checking</c>, <see cref="City"/> obrigatório,
/// <see cref="DocType"/> ∈ {<c>ci</c>, <c>ce</c>, <c>passport</c>, <c>ruc</c>},
/// <see cref="DocCountryCode"/> = <c>EC</c>.</description></item>
/// <item><description>PAUSD (PA): <see cref="Type"/> = <c>checking</c>, <see cref="DocType"/> ∈
/// {<c>ce</c>, <c>passport</c>, <c>ruc</c>}, <see cref="DocCountryCode"/> = <c>PA</c>.</description></item>
/// <item><description>PEN/PEUSD (PE): <see cref="AccountNumber"/> com exatamente 20 dígitos,
/// <see cref="Type"/> = <c>checking</c>, <see cref="City"/> obrigatório, <see cref="DocType"/> =
/// <c>DNI</c> aceito, <see cref="DocCountryCode"/> = <c>PE</c>.</description></item>
/// <item><description>PYG (PY): <see cref="DocType"/> = <c>CI</c> aceito,
/// <see cref="DocCountryCode"/> = <c>PY</c>. Sem <see cref="Type"/>/<see cref="City"/>.</description></item>
/// <item><description>UYU (UY): <see cref="Type"/> = <c>checking</c>, <see cref="DocType"/> = <c>CI</c>
/// aceito, <see cref="DocCountryCode"/> = <c>UY</c>.</description></item>
/// </list>
/// </description></item>
/// </list>
/// <see cref="Iban"/> permanece não confirmado individualmente (nenhum rail testado o exigiu).
/// </remarks>
public sealed record RecipientAccount
{
    /// <summary>Rail de pagamento. Discrimina quais dos demais campos são exigidos.</summary>
    public required AccountType AccountType { get; init; }

    /// <summary>Endereço da carteira. Confirmado para <see cref="Models.AccountType.Wallet"/>.</summary>
    public string? Address { get; init; }

    /// <summary>Blockchain. Confirmado para <see cref="Models.AccountType.Wallet"/>.</summary>
    public WalletNetwork? Network { get; init; }

    /// <summary>Stablecoin. Confirmado para <see cref="Models.AccountType.Wallet"/>.</summary>
    public WalletToken? Token { get; init; }

    /// <summary>
    /// Nome do banco. Confirmado obrigatório para SWIFT; presente com sucesso também em
    /// ACH/WIRE/INSTANT_PAY/SPEI/CRC/GTQ/SVUSD. Ver remarks da classe para o payload completo por rail.
    /// </summary>
    public string? BankName { get; init; }

    /// <summary>
    /// Endereço do banco. Confirmado contra o sandbox em 2026-07-15 que a forma exigida depende do
    /// rail: <b>string solta</b> para ACH e INSTANT_PAY (ex.: <c>"270 Park Ave, New York, NY 10017,
    /// US"</c>); <b>objeto estruturado</b> (<see cref="KiraAddress"/>) para SWIFT e WIRE
    /// (<c>account.bank_address.street_name: Required</c> quando enviado como string —
    /// <c>"Expected object, received string"</c>). Corrige uma suposição anterior (2026-07-14) de
    /// que SWIFT aceitava string aqui.
    /// <para>
    /// Tipado como <see cref="object"/> em vez de duas propriedades separadas (uma <see cref="string"/>,
    /// uma <see cref="KiraAddress"/>) porque o serializador não permite duas propriedades C#
    /// mapeando para a mesma chave JSON (<c>bank_address</c>) — mesmo com uma delas sempre nula.
    /// Atribua um <see cref="string"/> para ACH/INSTANT_PAY, ou uma instância de
    /// <see cref="KiraAddress"/> para SWIFT/WIRE.
    /// </para>
    /// </summary>
    public object? BankAddress { get; init; }

    /// <summary>
    /// Código do banco. Formato validado pela Kira (2–4 dígitos; ARS explicitamente rejeita mais de
    /// 4), mas o <b>valor</b> precisa bater com uma tabela real por país que só é exposta via
    /// <c>GET /banks</c> — endpoint confirmado quebrado (corpo não-JSON) para todos os 15 países
    /// testados. É o único bloqueio restante para PSE/ARS/CLP/DOP/ECUSD/PAUSD/PEN/PEUSD/PYG/UYU; ver
    /// remarks da classe. Confirmado aceito sem reclamação para GTQ e SVUSD com o valor <c>001</c>.
    /// </summary>
    public string? BankCode { get; init; }

    /// <summary>
    /// Número da conta. Confirmado obrigatório para SWIFT/ACH/WIRE/INSTANT_PAY/GTQ/SVUSD/BRL.
    /// Formato validado por rail: exatamente 22 dígitos para CRC, exatamente 20 para PEN/PEUSD.
    /// </summary>
    public string? AccountNumber { get; init; }

    /// <summary>Routing number. Confirmado obrigatório para ACH/WIRE/INSTANT_PAY (mesmo payload nos três).</summary>
    public string? RoutingNumber { get; init; }

    /// <summary>BIC/SWIFT. Confirmado obrigatório para <see cref="Models.AccountType.Swift"/>.</summary>
    public string? SwiftCode { get; init; }

    /// <summary>IBAN. Não confirmado individualmente — nenhum rail testado o exigiu.</summary>
    public string? Iban { get; init; }

    /// <summary>CLABE (SPEI, México). Confirmado obrigatório — 201 real com <see cref="Clabe"/> + <see cref="BankName"/>.</summary>
    public string? Clabe { get; init; }

    /// <summary>
    /// Tipo do documento do beneficiário. Confirmado obrigatório em todo rail LATAM exceto SPEI, com
    /// um conjunto de valores aceitos por país (enum do lado Kira) — ver remarks da classe.
    /// </summary>
    /// <remarks>
    /// Corrige o nome de campo assumido antes (2026-07-14): a chave real é <c>doc_type</c>, não
    /// <c>document_type</c> — confirmado via erro de validação (<c>"account.doc_type: Required"</c>
    /// mesmo com <c>document_type</c> presente no corpo).
    /// </remarks>
    public string? DocType { get; init; }

    /// <summary>
    /// Número do documento do beneficiário. Confirmado obrigatório em todo rail LATAM exceto SPEI.
    /// ARS exige exatamente 11 dígitos (CUIL/CUIT).
    /// </summary>
    /// <remarks>
    /// Corrige o nome de campo assumido antes (2026-07-14): a chave real é <c>doc_number</c>, não
    /// <c>document_number</c> — mesmo erro de validação que <see cref="DocType"/>.
    /// </remarks>
    public string? DocNumber { get; init; }

    /// <summary>
    /// Código do país do documento (ISO 3166-1 alpha-2). Confirmado obrigatório em todo rail LATAM
    /// exceto SPEI; quando a Kira valida um literal fixo (AR/BR/CL/CR/PY confirmados via mensagem de
    /// erro), precisa bater exatamente com o país do rail — não é livre.
    /// </summary>
    public string? DocCountryCode { get; init; }

    /// <summary>
    /// Cidade do beneficiário. Confirmado obrigatório para BRL/ECUSD/PEN/PEUSD; ausente/opcional nos
    /// demais rails LATAM testados (nenhum erro sem o campo).
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Sub-tipo de conta local — campo <c>type</c>, distinto do discriminador <see cref="AccountType"/>
    /// (que serializa para <c>account_type</c>). Confirmado obrigatório para PSE/ARS/CLP/DOP/ECUSD/
    /// PAUSD/PEN/PEUSD/UYU; ausente/opcional em CRC/GTQ/PYG/SVUSD/BRL. Valores aceitos variam por
    /// país — só CLP tem o enum completo confirmado (<c>Cuenta corriente</c>/<c>Cuenta de ahorros</c>/
    /// <c>Cuenta Vista</c>, strings exatas em espanhol); os demais aceitaram <c>checking</c> sem
    /// reclamação, mas o enum completo não foi exposto (bloqueados antes por <see cref="BankCode"/>).
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Tipo de chave PIX. Confirmado obrigatório para BRL, com enum exposto pela Kira:
    /// <c>code_cpf</c> (usado no 201 confirmado), <c>code_cnpj</c>, <c>email</c>, <c>random_key</c>,
    /// <c>phone_number</c>.
    /// </summary>
    public string? PixKeyType { get; init; }

    /// <summary>Chave PIX. Presente no payload que resultou em 201 para BRL; obrigatoriedade não isolada individualmente.</summary>
    public string? PixKey { get; init; }
}

/// <summary>
/// Recipient devolvido pela Kira. Confirmado idêntico em <c>POST</c> e <c>GET /v1/recipients/{id}</c>.
/// </summary>
public sealed record KiraRecipient : KiraResponse
{
    /// <summary>Identificador do recipient. Campo real é <c>recipient_id</c>, não <c>id</c>.</summary>
    [JsonPropertyName("recipient_id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Usuário dono. Não confirmado presente na resposta — <c>user_id</c> não apareceu em nenhum
    /// teste de criação/consulta contra o sandbox; mantido nulo por padrão.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>Pessoa física ou jurídica. Valores observados: <c>individual</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Nome do beneficiário, para pessoa física.</summary>
    public string? FirstName { get; init; }

    /// <summary>Sobrenome do beneficiário, para pessoa física.</summary>
    public string? LastName { get; init; }

    /// <summary>Razão social do beneficiário, para pessoa jurídica.</summary>
    public string? CompanyName { get; init; }

    /// <summary>Rail de pagamento.</summary>
    public AccountType? AccountType { get; init; }

    /// <summary>Dados da conta — a forma varia por <see cref="AccountType"/>, como no request.</summary>
    public RecipientAccountDetails? AccountDetails { get; init; }

    /// <summary>
    /// Criação. Campo real é <c>created_ts</c>, não <c>created_at</c>, e em um formato que o
    /// conversor padrão de <see cref="DateTimeOffset"/> rejeita — ver <see cref="KiraTimestampConverter"/>.
    /// </summary>
    [JsonPropertyName("created_ts")]
    [JsonConverter(typeof(KiraTimestampConverter))]
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Última atualização. Campo real é <c>updated_ts</c>, não <c>updated_at</c>.</summary>
    [JsonPropertyName("updated_ts")]
    [JsonConverter(typeof(KiraTimestampConverter))]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>Dados da conta de um <see cref="KiraRecipient"/>. Confirmado para o rail <c>WALLET</c>.</summary>
public sealed record RecipientAccountDetails : KiraResponse
{
    /// <summary>Endereço da carteira, para recipients cripto.</summary>
    public string? Address { get; init; }

    /// <summary>Blockchain, para recipients cripto.</summary>
    public WalletNetwork? Network { get; init; }

    /// <summary>Stablecoin, para recipients cripto.</summary>
    public WalletToken? Token { get; init; }
}

/// <summary>
/// Envelope real de <c>GET /v1/recipients?user_id=</c>: <c>{"recipients": [...], "total": N}</c>.
/// </summary>
/// <remarks>
/// Confirmado contra o sandbox em 2026-07-14 — nem array solto na raiz, nem o envelope
/// <c>{data, pagination}</c> usado por <c>GET /v1/users</c> e <c>GET /v1/virtual-accounts</c>.
/// Mais um formato de envelope distinto, por família de endpoint.
/// </remarks>
internal sealed record KiraRecipientsEnvelope(IReadOnlyList<KiraRecipient> Recipients, int? Total);
