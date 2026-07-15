using System.Net;
using CambioReal.Kira.Models;
using CambioReal.Kira.Tests.Fakes;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

/// <summary>Cobre os recursos que <see cref="ResourceTests"/> não exercita.</summary>
public sealed class ResourceCoverageTests
{
    private const string Root = "https://api.balampay.com/sandbox/";

    [Fact]
    public async Task CreateVerificationPostsUnderTheUser()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"id":"v-1","verification_link":"https://kyc.example/x","status":"pending"}"""));

        var verification = await client.Users.CreateVerificationAsync(
            "u-1",
            new CreateVerificationRequest { Method = "embedded-link", RedirectUri = new Uri("https://app.example/done") });

        verification.Status.ShouldBe(VerificationStatus.Pending);
        verification.VerificationLink.ShouldBe(new Uri("https://kyc.example/x"));

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/users/u-1/verifications");
        request.IdempotencyKey.ShouldNotBeNull();
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"redirect_uri\":\"https://app.example/done\"");
    }

    /// <summary>
    /// Formato real confirmado contra o sandbox em 2026-07-14 (criado e verificado ponta a
    /// ponta): os dados da conta vão aninhados em <c>account</c>, com <c>address</c>/<c>network</c>/
    /// <c>token</c> sem o prefixo <c>wallet_</c>; e a resposta usa <c>recipient_id</c> +
    /// <c>account_details</c>, não <c>id</c> + campos flat.
    /// </summary>
    [Fact]
    public async Task CreateWalletRecipientSerializesNetworkAndToken()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"recipient_id":"r-1","account_type":"WALLET","account_details":{"network":"solana","token":"USDT"}}"""));

        var recipient = await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            FirstName = "Kira",
            LastName = "Recipient",
            Account = new RecipientAccount
            {
                AccountType = AccountType.Wallet,
                Address = "So1anaAddr",
                Network = WalletNetwork.Solana,
                Token = WalletToken.Usdt,
            },
        });

        recipient.Id.ShouldBe("r-1");
        recipient.AccountType.ShouldBe(AccountType.Wallet);
        recipient.AccountDetails!.Network.ShouldBe(WalletNetwork.Solana);
        recipient.AccountDetails.Token.ShouldBe(WalletToken.Usdt);

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/recipients");
        request.IdempotencyKey.ShouldNotBeNull();
        request.Body.ShouldNotBeNull();

        // Três convenções de casing no mesmo corpo.
        request.Body.ShouldContain("\"account_type\":\"WALLET\"");
        request.Body.ShouldContain("\"network\":\"solana\"");
        request.Body.ShouldContain("\"token\":\"USDT\"");
    }

    /// <summary>
    /// Criação real, ponta a ponta, confirmada contra o sandbox em 2026-07-15: <c>bank_address</c>
    /// exige um <b>objeto</b> estruturado para SWIFT (<c>"Expected object, received string"</c> se
    /// enviado como string — correção da suposição de 2026-07-14, que assumia string) e o endereço
    /// do beneficiário na raiz (<c>address</c>) também é objeto e obrigatório.
    /// </summary>
    [Fact]
    public async Task CreateSwiftRecipientOmitsOptionalContactFields()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"recipient_id":"r-2","account_type":"SWIFT"}"""));

        await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            CompanyName = "Acme Ltd",
            Address = new KiraAddress { StreetName = "123 Main St", City = "New York", State = "NY", PostalCode = "10001", Country = "US" },
            Account = new RecipientAccount
            {
                AccountType = AccountType.Swift,
                AccountNumber = "0001234567",
                SwiftCode = "DEUTDEFF",
                BankName = "Deutsche Bank",
                BankAddress = new KiraAddress { StreetName = "Taunusanlage 12", City = "Frankfurt", State = "HE", PostalCode = "60325", Country = "DE" },
                Iban = "DE89370400440532013000",
            },
        });

        var body = transport.Requests.Single().Body;
        body.ShouldNotBeNull();
        body.ShouldContain("\"swift_code\":\"DEUTDEFF\"");
        body.ShouldContain("\"company_name\":\"Acme Ltd\"");
        body.ShouldContain("\"bank_address\":{\"street_name\":\"Taunusanlage 12\"");
        body.ShouldContain("\"address\":{\"street_name\":\"123 Main St\"");

        // Opcionais; não devem ir como null.
        body.ShouldNotContain("email");
        body.ShouldNotContain("phone");
    }

    /// <summary>
    /// Confirmado contra o sandbox em 2026-07-15: para ACH, <c>bank_address</c> é uma <b>string</b>
    /// solta (ao contrário de SWIFT/WIRE, que exigem objeto), mas o <c>address</c> do beneficiário na
    /// raiz é objeto estruturado igual aos demais rails domésticos dos EUA.
    /// </summary>
    [Fact]
    public async Task CreateAchRecipientUsesStringBankAddressWithStructuredRootAddress()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"recipient_id":"r-3","account_type":"ACH"}"""));

        await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            FirstName = "Kira",
            LastName = "Recipient",
            Address = new KiraAddress { StreetName = "123 Main St", City = "New York", State = "NY", PostalCode = "10001", Country = "US" },
            Account = new RecipientAccount
            {
                AccountType = AccountType.Ach,
                AccountNumber = "000123456789",
                RoutingNumber = "021000021",
                BankName = "JPMorgan Chase",
                BankAddress = "270 Park Ave, New York, NY 10017, US",
            },
        });

        var body = transport.Requests.Single().Body;
        body.ShouldNotBeNull();
        body.ShouldContain("\"bank_address\":\"270 Park Ave, New York, NY 10017, US\"");
        body.ShouldContain("\"address\":{\"street_name\":\"123 Main St\"");
    }

    /// <summary>
    /// INSTANT_PAY foi criado com sucesso (201) contra o sandbox em 2026-07-15, apesar da
    /// contradição de documentação registrada em <see cref="AccountType"/> (a API Reference do
    /// <c>createRecipient</c> não lista INSTANT_PAY entre os tipos aceitos). Payload igual ao de ACH.
    /// </summary>
    [Fact]
    public async Task CreateInstantPayRecipientIsAcceptedDespiteDocumentedContradiction()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"recipient_id":"r-4","account_type":"INSTANT_PAY"}"""));

        var recipient = await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            FirstName = "Kira",
            LastName = "Recipient",
            Address = new KiraAddress { StreetName = "123 Main St", City = "New York", State = "NY", PostalCode = "10001", Country = "US" },
            Account = new RecipientAccount
            {
                AccountType = AccountType.InstantPay,
                AccountNumber = "000123456789",
                RoutingNumber = "021000021",
                BankName = "JPMorgan Chase",
                BankAddress = "270 Park Ave, New York, NY 10017, US",
            },
        });

        recipient.AccountType.ShouldBe(AccountType.InstantPay);
        var body = transport.Requests.Single().Body;
        body.ShouldNotBeNull();
        body.ShouldContain("\"account_type\":\"INSTANT_PAY\"");
    }

    /// <summary>
    /// Confirmado contra o sandbox em 2026-07-15: BRL usa chave PIX, não os campos bancários
    /// genéricos. A chave real dos campos de documento é <c>doc_type</c>/<c>doc_number</c>, não
    /// <c>document_type</c>/<c>document_number</c> (suposição de 2026-07-14, nunca confirmada e
    /// agora corrigida) — a Kira rejeitava <c>document_type</c> com <c>"doc_type: Required"</c>.
    /// </summary>
    [Fact]
    public async Task CreateBrlRecipientSerializesPixAndDocFields()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"recipient_id":"r-5","account_type":"BRL"}"""));

        await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            FirstName = "Kira",
            LastName = "Recipient",
            Account = new RecipientAccount
            {
                AccountType = AccountType.Brl,
                AccountNumber = "000123456789",
                PixKeyType = "code_cpf",
                PixKey = "12345678909",
                City = "Sao Paulo",
                DocType = "cpf",
                DocNumber = "12345678909",
                DocCountryCode = "BR",
            },
        });

        var body = transport.Requests.Single().Body;
        body.ShouldNotBeNull();
        body.ShouldContain("\"pix_key_type\":\"code_cpf\"");
        body.ShouldContain("\"pix_key\":\"12345678909\"");
        body.ShouldContain("\"doc_type\":\"cpf\"");
        body.ShouldContain("\"doc_number\":\"12345678909\"");
        body.ShouldContain("\"doc_country_code\":\"BR\"");
        body.ShouldNotContain("document_type");
        body.ShouldNotContain("document_number");
    }

    /// <summary>
    /// Envelope real confirmado contra o sandbox em 2026-07-14: <c>{"recipients": [...], "total": N}</c>
    /// — nem array solto na raiz, nem o envelope <c>{data, pagination}</c> de outras listagens.
    /// </summary>
    [Fact]
    public async Task ListRecipientsByUserFiltersByQuery()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"recipients":[{"recipient_id":"r-1"},{"recipient_id":"r-2"}],"total":2}""");

        var recipients = await client.Recipients.ListByUserAsync("u-1");

        recipients.Count.ShouldBe(2);
        recipients[0].Id.ShouldBe("r-1");
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/recipients?user_id=u-1");
    }

    [Fact]
    public async Task GetRecipientUsesThePathSegment()
    {
        var (client, transport) = TestClient.CreateOk("""{"recipient_id":"r-1","account_type":"ACH"}""");

        var recipient = await client.Recipients.GetAsync("r-1");

        recipient.Id.ShouldBe("r-1");
        recipient.AccountType.ShouldBe(AccountType.Ach);
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/recipients/r-1");
    }

    [Fact]
    public async Task ListVirtualAccountsEncodesEnumFilters()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"data":[{"id":"va-1"}],"pagination":{"total":1,"limit":10,"offset":0,"has_more":false}}""");

        var page = await client.VirtualAccounts.ListAsync(new ListVirtualAccountsRequest
        {
            Status = VirtualAccountStatus.Active,
            Mode = VirtualAccountMode.Crypto,
            Search = "acme corp",
        });

        page.Data.Count.ShouldBe(1);
        page.Pagination!.Total.ShouldBe(1);
        page.Pagination.HasMore.ShouldBe(false);

        // AbsoluteUri, não ToString(): ToString() devolve a forma de exibição e desfaz o %20
        // (embora preserve o %2F, para não alterar a semântica do path). O que trafega é o escapado.
        var uri = transport.Requests.Single().RequestUri!.AbsoluteUri;
        uri.ShouldContain("status=active");
        uri.ShouldContain("mode=crypto");
        uri.ShouldContain("search=acme%20corp");
    }

    [Fact]
    public async Task GetVirtualAccountReadsDepositInstructions()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"id":"va-1","status":"active","type":"US_BANK","deposit_instructions":{"bank_name":"Portage","routing_number":"021000021"}}""");

        var account = await client.VirtualAccounts.GetAsync("va-1");

        account.Type.ShouldBe("US_BANK");
        account.DepositInstructions.ShouldNotBeNull();
        account.DepositInstructions.BankName.ShouldBe("Portage");
        account.DepositInstructions.RoutingNumber.ShouldBe("021000021");

        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/virtual-accounts/va-1");
    }

    /// <summary>
    /// <c>GET</c> e <c>POST</c> em <c>v1/users/{id}/virtual-accounts</c> compartilham o path;
    /// só o método os distingue.
    /// </summary>
    [Fact]
    public async Task ListVirtualAccountsByUserUsesGetOnTheCreationPath()
    {
        var (client, transport) = TestClient.CreateOk("""[{"id":"va-1","mode":"fiat"}]""");

        var accounts = await client.VirtualAccounts.ListByUserAsync("u-1");

        accounts.Single().Mode.ShouldBe(VirtualAccountMode.Fiat);

        var request = transport.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.ToString().ShouldBe(Root + "v1/users/u-1/virtual-accounts");
        request.IdempotencyKey.ShouldBeNull();
    }

    [Fact]
    public async Task ListDepositsReadsEnrichedFields()
    {
        var (client, transport) = TestClient.CreateOk(
            """[{"id":"d-1","amount":"500.00","currency":"USD","payment_rail":"ACH","sender_name":"Acme","settlement_transaction_hash":"0xdead"}]""");

        var deposits = await client.VirtualAccounts.ListDepositsAsync("va-1");

        var deposit = deposits.Single();
        deposit.Amount.ShouldBe(500m);
        deposit.Currency.ShouldBe(Currency.Usd);
        deposit.PaymentRail.ShouldBe("ACH");
        deposit.SenderName.ShouldBe("Acme");
        deposit.SettlementTransactionHash.ShouldBe("0xdead");

        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/virtual-accounts/va-1/deposits");
    }

    [Fact]
    public async Task GetDepositEscapesBothIdentifiers()
    {
        var (client, transport) = TestClient.CreateOk("""{"id":"d/1","amount":"1.00"}""");

        await client.VirtualAccounts.GetDepositAsync("va/1", "d/1");

        transport.Requests.Single().RequestUri!.ToString()
            .ShouldBe(Root + "v1/virtual-accounts/va%2F1/deposits/d%2F1");
    }

    [Fact]
    public async Task GetLiquidationAddressReadsNetworkAndRecipient()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"id":"la-1","address":"So1ana","network":"solana","token":"USDC","recipient_id":"r-1"}""");

        var address = await client.VirtualAccounts.GetLiquidationAddressAsync("va-1");

        address.Network.ShouldBe(WalletNetwork.Solana);
        address.Token.ShouldBe(WalletToken.Usdc);
        address.RecipientId.ShouldBe("r-1");

        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/virtual-accounts/va-1/liquidation-address");
    }

    [Fact]
    public async Task CalculatePayInFeesReadsTheFullBreakdown()
    {
        var (client, transport) = TestClient.CreateOk(
            """
            {"collection_fees":{"fixed_fee":"5.00","percentage_fee":"0.5"},
             "settlement_fees":{"fixed_fee":"20.00","percentage_fee":"0"},
             "rate":"18.00","kira_rate":"17.10","fx_markup":"5","final_amount":"26.39"}
            """);

        var fees = await client.PayIns.CalculateFeesAsync(new PayInFeesRequest
        {
            Method = PayInMethod.Spei,
            Amount = 500m,
            Currency = Currency.Mxn,
        });

        fees.CollectionFees!.FixedFee.ShouldBe(5m);
        fees.CollectionFees.PercentageFee.ShouldBe(0.5m);
        fees.SettlementFees!.FixedFee.ShouldBe(20m);
        fees.Rate.ShouldBe(18m);
        fees.KiraRate.ShouldBe(17.10m);
        fees.FinalAmount.ShouldBe(26.39m);

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/payins/fees");
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"method\":\"SPEI\"");
        request.Body.ShouldContain("\"currency\":\"MXN\"");
        request.IdempotencyKey.ShouldBeNull(); // cálculo não cria nada
    }

    /// <summary>
    /// Confirmado contra o sandbox em 2026-07-14 com uma criação real bem-sucedida
    /// (<c>country_code=BR</c>/<c>currency=BRL</c>): <c>client_uuid</c>, <c>reference</c> e
    /// <c>country_code</c> são obrigatórios, e a resposta usa <c>txn_uuid</c>/<c>payment_link</c>,
    /// não <c>id</c>/<c>url</c>.
    /// </summary>
    [Fact]
    public async Task CreatePaymentLinkSerializesRedirectUri()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"txn_uuid":"pl-1","payment_link":"https://pay.example/pl-1","status":"INIT","payment_type":"CASH"}"""));

        var link = await client.PaymentLinks.CreateAsync(new CreatePaymentLinkRequest
        {
            ClientUuid = "c-1",
            Amount = 100m,
            Currency = Currency.Usd,
            CountryCode = "BR",
            Reference = "ref-1",
            RedirectUri = new Uri("https://app.example/thanks"),
        });

        link.Id.ShouldBe("pl-1");
        link.Url.ShouldBe(new Uri("https://pay.example/pl-1"));
        link.PaymentType.ShouldBe("CASH");

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/payment-link");
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"redirect_uri\":\"https://app.example/thanks\"");
        request.Body.ShouldContain("\"currency\":\"USD\"");
        request.Body.ShouldContain("\"country_code\":\"BR\"");
        request.Body.ShouldContain("\"client_uuid\":\"c-1\"");
    }

    [Fact]
    public async Task ListCountriesReadsSubdivisions()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"count":1,"data":[{"alpha3":"BRA","name":"Brazil","subdivisions":[{"code":"SP","name":"Sao Paulo"}]}]}""");

        var countries = await client.Platform.ListCountriesAsync();

        var brazil = countries.Single();
        brazil.Code.ShouldBe("BRA");
        brazil.Subdivisions.Single().Code.ShouldBe("SP");

        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/countries");
    }
}
