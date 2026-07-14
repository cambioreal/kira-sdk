using System.Text.Json;
using CambioReal.Kira.Models;
using CambioReal.Kira.Serialization;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

/// <summary>
/// A Kira mistura quatro convenções de casing nos valores de enum. Estes testes travam cada uma —
/// um conversor global uniformizaria tudo e quebraria três delas em silêncio.
/// </summary>
public sealed class SerializationTests
{
    [Theory]
    [InlineData(AccountType.Ach, "ACH")]
    [InlineData(AccountType.Wire, "WIRE")]
    [InlineData(AccountType.Swift, "SWIFT")]
    [InlineData(AccountType.Wallet, "WALLET")]
    [InlineData(AccountType.Spei, "SPEI")]
    [InlineData(AccountType.InstantPay, "INSTANT_PAY")]
    [InlineData(AccountType.Brl, "BRL")]
    [InlineData(AccountType.Ecusd, "ECUSD")]
    public void AccountTypeSerializesAsScreamingSnake(AccountType value, string expected) =>
        Serialize(value).ShouldBe(expected);

    [Theory]
    [InlineData(VerificationMode.Automatic, "automatic")]
    [InlineData(VerificationMode.VerificationLink, "verification_link")]
    public void VerificationModeSerializesAsSnakeCase(VerificationMode value, string expected) =>
        Serialize(value).ShouldBe(expected);

    [Theory]
    [InlineData(VirtualAccountProvider.Portage, "portage")]
    [InlineData(VirtualAccountProvider.AustinCapitalTrust, "austin_capital_trust")]
    [InlineData(VirtualAccountProvider.SlovakSavingsBank, "slovak_savings_bank")]
    public void ProviderSerializesAsSnakeCase(VirtualAccountProvider value, string expected) =>
        Serialize(value).ShouldBe(expected);

    [Theory]
    [InlineData(KiraProduct.UsaVirtualAccounts, "usa-virtual-accounts")]
    [InlineData(KiraProduct.UsaVirtualAccountsAct, "usa-virtual-accounts-act")]
    public void ProductSerializesAsKebabCase(KiraProduct value, string expected) =>
        Serialize(value).ShouldBe(expected);

    [Theory]
    [InlineData(KycType.Full, "Full")]
    [InlineData(KycType.Simplified, "Simplified")]
    public void KycTypeKeepsPascalCase(KycType value, string expected) =>
        Serialize(value).ShouldBe(expected);

    [Theory]
    [InlineData(Currency.Usd, "USD")]
    [InlineData(Currency.Usdc, "USDC")]
    [InlineData(Currency.Brl, "BRL")]
    public void CurrencySerializesUppercase(Currency value, string expected) =>
        Serialize(value).ShouldBe(expected);

    [Fact]
    public void RequestBodyUsesSnakeCaseProperties()
    {
        var json = JsonSerializer.Serialize(
            new CreateUserRequest
            {
                Type = UserType.Business,
                VerificationMode = VerificationMode.VerificationLink,
                BusinessLegalName = "Acme",
                AddressCountry = "BRA",
            },
            KiraJson.Options);

        json.ShouldContain("\"business_legal_name\":\"Acme\"");
        json.ShouldContain("\"verification_mode\":\"verification_link\"");
        json.ShouldContain("\"address_country\":\"BRA\"");
        json.ShouldContain("\"type\":\"business\"");

        // WhenWritingNull: campos não preenchidos não vão no corpo.
        json.ShouldNotContain("first_name");
    }

    /// <summary>Valores monetários trafegam como string, como nos exemplos da Kira ("1000.00").</summary>
    [Fact]
    public void MoneyIsWrittenAsString()
    {
        var json = JsonSerializer.Serialize(
            new InitiatePayoutRequest { RecipientId = "r-1", Amount = 1000.50m },
            KiraJson.Options);

        json.ShouldContain("\"amount\":\"1000.50\"");
    }

    /// <summary>
    /// Formato real de timestamp em <c>KiraRecipient</c>, confirmado contra o sandbox em
    /// 2026-07-14: espaço em vez de <c>T</c>, offset de 2 dígitos sem separador de minutos. O
    /// conversor padrão de <see cref="DateTimeOffset"/> rejeita essa forma.
    /// </summary>
    [Fact]
    public void RecipientTimestampParsesTheNonStandardKiraFormat()
    {
        var json = """{"recipient_id":"r-1","created_ts":"2026-07-14 01:29:21.851995+00"}""";

        var recipient = JsonSerializer.Deserialize<KiraRecipient>(json, KiraJson.Options);

        recipient.ShouldNotBeNull();
        recipient.CreatedAt.ShouldBe(new DateTimeOffset(2026, 7, 14, 1, 29, 21, 851, TimeSpan.Zero).AddTicks(9950));
    }

    /// <summary>E é lido tanto de string quanto de número.</summary>
    [Theory]
    [InlineData("""{"amount":"1000.50","currency":"USD"}""")]
    [InlineData("""{"amount":1000.50,"currency":"USD"}""")]
    public void MoneyIsReadFromStringOrNumber(string json)
    {
        var balance = JsonSerializer.Deserialize<VirtualAccountBalance>(json, KiraJson.Options);

        balance.ShouldNotBeNull();
        balance.Amount.ShouldBe(1000.50m);
        balance.Currency.ShouldBe(Currency.Usd);
    }

    /// <summary>
    /// Campos que a Kira devolve e o SDK não modela não podem desaparecer: a doc é incompleta,
    /// e perder um campo em silêncio é pior do que não tê-lo.
    /// </summary>
    [Fact]
    public void UnknownResponseFieldsAreCaptured()
    {
        var json = """{"id":"u-1","undocumented_field":42,"nested":{"a":1}}""";

        var user = JsonSerializer.Deserialize<KiraUser>(json, KiraJson.Options);

        user.ShouldNotBeNull();
        user.Id.ShouldBe("u-1");
        user.AdditionalData.ShouldContainKey("undocumented_field");
        user.AdditionalData["undocumented_field"].GetInt32().ShouldBe(42);
        user.AdditionalData.ShouldContainKey("nested");
    }

    /// <summary>
    /// Formato real confirmado contra o sandbox em 2026-07-13 (<c>POST /v1/users</c>):
    /// <c>eligible_products</c> é uma lista de objetos, com <c>missing_fields</c> por produto —
    /// não a lista de enum nem o campo <c>missing_fields</c> na raiz que a doc em prosa sugeria.
    /// </summary>
    [Fact]
    public void EligibleProductsDeserializeAsRichObjects()
    {
        var json = """
            {"id":"u-1","eligible_products":[
                {"product_id":"usa-virtual-accounts-act","product_code":"usa-virtual-accounts-act","product_name":"USA Virtual Accounts (ACT)","eligible":false,"missing_fields":["ssn"]},
                {"product_id":"usa-virtual-accounts-zenus","product_code":"usa-virtual-accounts-zenus","product_name":"USA Virtual Accounts (Zenus)","eligible":false,"missing_fields":["birth_date"]}
            ]}
            """;

        var user = JsonSerializer.Deserialize<KiraUser>(json, KiraJson.Options);

        user.ShouldNotBeNull();
        user.EligibleProducts.Count.ShouldBe(2);
        user.EligibleProducts[0].ProductId.ShouldBe("usa-virtual-accounts-act");
        user.EligibleProducts[0].Eligible.ShouldBe(false);
        user.EligibleProducts[0].MissingFields.ShouldBe(["ssn"]);
        // O terceiro produto (zenus) não é documentado nem está em nenhuma versão anterior do
        // enum KiraProduct — mas por ProductId ser string, não quebra a desserialização.
        user.EligibleProducts[1].ProductId.ShouldBe("usa-virtual-accounts-zenus");
    }

    [Theory]
    [InlineData("RUS", true)]
    [InlineData("VEN", true)]
    [InlineData("ukr", true)]
    [InlineData("BRA", false)]
    [InlineData("USA", false)]
    [InlineData(null, false)]
    public void RestrictedCountriesAreRecognised(string? code, bool expected) =>
        KiraRestrictedCountries.IsRestricted(code).ShouldBe(expected);

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, KiraJson.Options).Trim('"');
}
