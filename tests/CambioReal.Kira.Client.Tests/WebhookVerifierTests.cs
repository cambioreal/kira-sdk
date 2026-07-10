using System.Security.Cryptography;
using System.Text;
using CambioReal.Kira.Webhooks;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

public sealed class WebhookVerifierTests
{
    private const string Secret = "whsec_test";
    private static readonly byte[] Payload = Encoding.UTF8.GetBytes("""{"event":"user.created","event_id":"e-1"}""");

    [Fact]
    public void AcceptsHexSignature()
    {
        KiraWebhookVerifier.IsValid(Payload, HexSignature(Payload), Secret).ShouldBeTrue();
    }

    [Fact]
    public void AcceptsHexSignatureWithSchemePrefix()
    {
        KiraWebhookVerifier.IsValid(Payload, "sha256=" + HexSignature(Payload), Secret).ShouldBeTrue();
    }

    [Fact]
    public void AcceptsBase64Signature()
    {
        var digest = HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Payload);

        KiraWebhookVerifier.IsValid(Payload, Convert.ToBase64String(digest), Secret).ShouldBeTrue();
    }

    [Fact]
    public void RejectsTamperedPayload()
    {
        var signature = HexSignature(Payload);
        var tampered = Encoding.UTF8.GetBytes("""{"event":"user.created","event_id":"e-2"}""");

        KiraWebhookVerifier.IsValid(tampered, signature, Secret).ShouldBeFalse();
    }

    [Fact]
    public void RejectsWrongSecret()
    {
        KiraWebhookVerifier.IsValid(Payload, HexSignature(Payload), "whsec_other").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-signature")]
    [InlineData("zz")]
    public void RejectsMalformedSignature(string? signature)
    {
        KiraWebhookVerifier.IsValid(Payload, signature, Secret).ShouldBeFalse();
    }

    private static string HexSignature(byte[] payload) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), payload)).ToLowerInvariant();
}
