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

    [Fact]
    public async Task CreateWalletRecipientSerializesNetworkAndToken()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"id":"r-1","account_type":"WALLET","wallet_network":"solana","wallet_token":"USDT"}"""));

        var recipient = await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            AccountType = AccountType.Wallet,
            WalletAddress = "So1anaAddr",
            WalletNetwork = WalletNetwork.Solana,
            WalletToken = WalletToken.Usdt,
        });

        recipient.AccountType.ShouldBe(AccountType.Wallet);
        recipient.WalletNetwork.ShouldBe(WalletNetwork.Solana);
        recipient.WalletToken.ShouldBe(WalletToken.Usdt);

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/recipients");
        request.IdempotencyKey.ShouldNotBeNull();
        request.Body.ShouldNotBeNull();

        // Três convenções de casing no mesmo corpo.
        request.Body.ShouldContain("\"account_type\":\"WALLET\"");
        request.Body.ShouldContain("\"wallet_network\":\"solana\"");
        request.Body.ShouldContain("\"wallet_token\":\"USDT\"");
    }

    [Fact]
    public async Task CreateSwiftRecipientOmitsOptionalContactFields()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"id":"r-2","account_type":"SWIFT"}"""));

        await client.Recipients.CreateAsync(new CreateRecipientRequest
        {
            UserId = "u-1",
            AccountType = AccountType.Swift,
            AccountHolderName = "Acme Ltd",
            SwiftCode = "DEUTDEFF",
            Iban = "DE89370400440532013000",
        });

        var body = transport.Requests.Single().Body;
        body.ShouldNotBeNull();
        body.ShouldContain("\"swift_code\":\"DEUTDEFF\"");

        // Opcionais desde 2026-04-14; não devem ir como null.
        body.ShouldNotContain("email");
        body.ShouldNotContain("phone");
        body.ShouldNotContain("address_street");
    }

    [Fact]
    public async Task ListRecipientsByUserFiltersByQuery()
    {
        var (client, transport) = TestClient.CreateOk("""[{"id":"r-1"},{"id":"r-2"}]""");

        var recipients = await client.Recipients.ListByUserAsync("u-1");

        recipients.Count.ShouldBe(2);
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/recipients?user_id=u-1");
    }

    [Fact]
    public async Task GetRecipientUsesThePathSegment()
    {
        var (client, transport) = TestClient.CreateOk("""{"id":"r-1","account_type":"ACH"}""");

        var recipient = await client.Recipients.GetAsync("r-1");

        recipient.AccountType.ShouldBe(AccountType.Ach);
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/recipients/r-1");
    }

    [Fact]
    public async Task ListVirtualAccountsEncodesEnumFilters()
    {
        var (client, transport) = TestClient.CreateOk("""{"data":[{"id":"va-1"}],"total":1}""");

        var page = await client.VirtualAccounts.ListAsync(new ListVirtualAccountsRequest
        {
            Status = VirtualAccountStatus.Active,
            Mode = VirtualAccountMode.Crypto,
            Search = "acme corp",
        });

        page.Data.Count.ShouldBe(1);
        page.Total.ShouldBe(1);

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

    [Fact]
    public async Task CreatePaymentLinkSerializesRedirectUri()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"id":"pl-1","url":"https://pay.example/pl-1","amount":"100.00","status":"active"}"""));

        var link = await client.PaymentLinks.CreateAsync(new CreatePaymentLinkRequest
        {
            UserId = "u-1",
            Amount = 100m,
            Currency = Currency.Usd,
            RedirectUri = new Uri("https://app.example/thanks"),
        });

        link.Url.ShouldBe(new Uri("https://pay.example/pl-1"));
        link.Amount.ShouldBe(100m);

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/payment-link");
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"redirect_uri\":\"https://app.example/thanks\"");
        request.Body.ShouldContain("\"currency\":\"USD\"");
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
