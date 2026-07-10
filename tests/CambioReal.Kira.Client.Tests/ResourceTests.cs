using System.Net;
using CambioReal.Kira.Models;
using CambioReal.Kira.Tests.Fakes;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

/// <summary>Verifica método, path, headers e corpo de cada recurso.</summary>
public sealed class ResourceTests
{
    private const string Root = "https://api.balampay.com/sandbox/";

    [Fact]
    public async Task CreateUserPostsToUsersWithIdempotencyKey()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"id":"u-1","verification_triggered":true}"""));
        var key = Guid.NewGuid();

        var user = await client.Users.CreateAsync(
            new CreateUserRequest { Type = UserType.Individual, Email = "m@hideaki.dev" },
            key);

        user.Id.ShouldBe("u-1");
        user.VerificationTriggered.ShouldBe(true);

        var request = transport.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.ToString().ShouldBe(Root + "v1/users");
        request.IdempotencyKey.ShouldBe(key.ToString("D"));
    }

    [Fact]
    public async Task CreateUserGeneratesAnIdempotencyKeyWhenOmitted()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"id":"u-1"}"""));

        await client.Users.CreateAsync(new CreateUserRequest { Type = UserType.Individual });

        Guid.TryParse(transport.Requests.Single().IdempotencyKey, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateUserUsesPatch()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.OK, """{"id":"u-1","requires_reverification":true}"""));

        var response = await client.Users.UpdateAsync("u-1", new UpdateUserRequest { Phone = "+5511999999999" });

        response.RequiresReverification.ShouldBe(true);
        transport.Requests.Single().Method.ShouldBe(HttpMethod.Patch);
    }

    [Fact]
    public async Task ListUsersEncodesEnumFiltersWithTheirOwnCasing()
    {
        var (client, transport) = TestClient.CreateOk("""{"data":[]}""");

        await client.Users.ListAsync(new ListUsersRequest
        {
            Type = UserType.Business,
            VerificationStatus = VerificationStatus.Verified,
            Limit = 50,
        });

        var uri = transport.Requests.Single().RequestUri!.ToString();
        uri.ShouldContain("type=business");
        uri.ShouldContain("verification_status=verified");
        uri.ShouldContain("limit=50");
    }

    [Fact]
    public async Task UserIdIsUrlEscapedInThePath()
    {
        var (client, transport) = TestClient.CreateOk("""{"id":"x"}""");

        await client.Users.GetAsync("u/../admin");

        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/users/u%2F..%2Fadmin");
    }

    [Fact]
    public async Task CreateVirtualAccountPostsUnderTheUser()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"id":"va-1","status":"activating","mode":"fiat"}"""));

        var account = await client.VirtualAccounts.CreateAsync(
            "u-1",
            new CreateVirtualAccountRequest { Provider = VirtualAccountProvider.SlovakSavingsBank });

        account.Status.ShouldBe(VirtualAccountStatus.Activating);
        account.Mode.ShouldBe(VirtualAccountMode.Fiat);

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/users/u-1/virtual-accounts");
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"provider\":\"slovak_savings_bank\"");
    }

    [Fact]
    public async Task FiatPayoutSendsTheOtpHeader()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"id":"p-1","status":"created","amount":"100.00"}"""));

        var payout = await client.VirtualAccounts.InitiatePayoutAsync(
            "va-1",
            new InitiatePayoutRequest { RecipientId = "r-1", Amount = 100m },
            otpCode: "123456");

        payout.Status.ShouldBe(PayoutStatus.Created);
        payout.Amount.ShouldBe(100m);

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/virtual-accounts/va-1/payout");
        request.ValidationCode.ShouldBe("123456");
        request.IdempotencyKey.ShouldNotBeNull();
    }

    [Fact]
    public async Task CryptoPayoutOmitsOtpAndCarriesSupportingDocuments()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"id":"p-2","status":"created","deposit_instructions":{"address":"0xabc","network":"polygon","token":"USDC"}}"""));

        var payout = await client.VirtualAccounts.InitiatePayoutAsync(
            "va-1",
            new InitiatePayoutRequest
            {
                RecipientId = "r-1",
                Amount = 250m,
                PaymentInstructions = new PaymentInstructions { Network = WalletNetwork.Polygon, Token = WalletToken.Usdc },
                SupportingDocuments =
                [
                    new SupportingDocument { Type = SupportingDocumentType.Invoice, File = "data:application/pdf;base64,AAA=" },
                ],
            });

        payout.DepositInstructions.ShouldNotBeNull();
        payout.DepositInstructions.Address.ShouldBe("0xabc");
        payout.DepositInstructions.Token.ShouldBe(WalletToken.Usdc);

        var request = transport.Requests.Single();
        request.ValidationCode.ShouldBeNull();
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"payment_instructions\"");
        request.Body.ShouldContain("\"supporting_documents\"");
        request.Body.ShouldContain("\"type\":\"invoice\"");
    }

    [Fact]
    public async Task PreviewPayoutHitsThePreviewSubResource()
    {
        var (client, transport) = TestClient.CreateOk(
            """{"amount":"1000.00","recipient_amount":"977.00","fees":{"total_fees":"23.00"},"quote_id":"q-1"}""");

        var preview = await client.VirtualAccounts.PreviewPayoutAsync(
            "va-1",
            new PreviewPayoutRequest { AccountType = AccountType.Swift, Amount = 1000m, CreateQuote = true });

        preview.RecipientAmount.ShouldBe(977m);
        preview.Fees!.TotalFees.ShouldBe(23m);
        preview.QuoteId.ShouldBe("q-1");

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "v1/virtual-accounts/va-1/payout/preview");
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"account_type\":\"SWIFT\"");
        request.IdempotencyKey.ShouldBeNull(); // preview não cria nada
    }

    [Fact]
    public async Task RegisterWebhookSkipsTheBearerToken()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created, """{"id":"wh-1"}"""));

        await client.Platform.RegisterWebhookAsync(new RegisterWebhookRequest
        {
            Url = new Uri("https://cambioreal.example/webhooks/kira"),
            SecretKey = "whsec_x",
        });

        var request = transport.Requests.Single();
        request.RequestUri!.ToString().ShouldBe(Root + "webhooks/register");
        request.ApiKey.ShouldBe("api-key");
        request.Authorization.ShouldBeNull();
    }

    [Fact]
    public async Task ListBanksPassesTheCountryQuery()
    {
        var (client, transport) = TestClient.CreateOk("""[{"code":"1007","name":"Bancolombia"}]""");

        var banks = await client.Platform.ListBanksAsync("COL");

        banks.Single().Name.ShouldBe("Bancolombia");
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "banks?country=COL");
    }

    [Fact]
    public async Task CreatePayInSendsMethodUppercased()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"id":"pi-1","method":"PSE","payment_link":"https://pay.example/x","settlement":[]}"""));

        var payIn = await client.PayIns.CreateAsync(new CreatePayInRequest
        {
            UserId = "u-1",
            Method = PayInMethod.Pse,
            Amount = 500m,
            BankCode = "1007",
        });

        payIn.Method.ShouldBe(PayInMethod.Pse);
        payIn.PaymentLink.ShouldBe(new Uri("https://pay.example/x"));

        var request = transport.Requests.Single();
        request.Body.ShouldNotBeNull();
        request.Body.ShouldContain("\"method\":\"PSE\"");
        request.Body.ShouldContain("\"amount\":\"500\"");
    }

    [Fact]
    public async Task GetPayInReadsMultipleSettlements()
    {
        var (client, _) = TestClient.CreateOk(
            """{"id":"pi-1","method":"SPEI","settlement":[{"amount":"26.39","currency":"USDT"},{"amount":"10.00","currency":"USDT"}]}""");

        var payIn = await client.PayIns.GetAsync("pi-1");

        payIn.Settlement.Count.ShouldBe(2);
        payIn.Settlement[0].Amount.ShouldBe(26.39m);
        payIn.Settlement[0].Currency.ShouldBe(Currency.Usdt);
    }

    [Fact]
    public async Task CreateLiquidationAddressPostsToTheSubResource()
    {
        var (client, transport) = TestClient.Create((HttpStatusCode.Created,
            """{"id":"la-1","address":"TXyz","network":"tron","token":"USDT"}"""));

        var address = await client.VirtualAccounts.CreateLiquidationAddressAsync(
            "va-1",
            new CreateLiquidationAddressRequest
            {
                Network = WalletNetwork.Tron,
                Token = WalletToken.Usdt,
                RecipientId = "r-1",
            });

        address.Address.ShouldBe("TXyz");
        address.Network.ShouldBe(WalletNetwork.Tron);

        transport.Requests.Single().RequestUri!.ToString()
            .ShouldBe(Root + "v1/virtual-accounts/va-1/liquidation-address");
    }

    [Fact]
    public async Task GetBalanceReadsAmountAndCurrency()
    {
        var (client, transport) = TestClient.CreateOk("""{"amount":"1500.25","currency":"USD"}""");

        var balance = await client.VirtualAccounts.GetBalanceAsync("va-1");

        balance.Amount.ShouldBe(1500.25m);
        balance.Currency.ShouldBe(Currency.Usd);
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "v1/virtual-accounts/va-1/balance");
    }

    [Fact]
    public async Task SendVerificationCodeHitsTheOtpEndpoint()
    {
        var (client, transport) = TestClient.CreateOk("""{"sent":true}""");

        var response = await client.Platform.SendVerificationCodeAsync(
            new SendVerificationCodeRequest { Email = "m@hideaki.dev" });

        response.Sent.ShouldBe(true);
        transport.Requests.Single().RequestUri!.ToString().ShouldBe(Root + "verification/send");
    }
}
