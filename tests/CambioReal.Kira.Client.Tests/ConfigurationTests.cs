using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void SandboxBaseAddressKeepsTrailingSlash()
    {
        KiraEnvironment.Sandbox.GetBaseAddress().ToString().ShouldBe("https://api.balampay.com/sandbox/");
        KiraEnvironment.Production.GetBaseAddress().ToString().ShouldBe("https://api.balampay.com/");
    }

    /// <summary>
    /// A regressão que este teste existe para impedir: sem a barra final no base address, o
    /// <see cref="Uri"/> descarta o segmento <c>/sandbox</c> e a requisição vai para produção.
    /// </summary>
    [Fact]
    public void RelativePathResolvesUnderSandboxPrefix()
    {
        var resolved = new Uri(KiraEnvironment.Sandbox.GetBaseAddress(), "v1/users");

        resolved.ToString().ShouldBe("https://api.balampay.com/sandbox/v1/users");
    }

    [Fact]
    public void BaseAddressWithoutTrailingSlashIsRejected()
    {
        var options = NewOptions();
        options.BaseAddress = new Uri("https://api.balampay.com/sandbox", UriKind.Absolute);

        var error = Should.Throw<InvalidOperationException>(options.Validate);

        error.Message.ShouldContain("barra final");
    }

    [Theory]
    [InlineData("", "pwd", "key")]
    [InlineData("cid", "", "key")]
    [InlineData("cid", "pwd", "")]
    public void MissingCredentialsAreRejected(string clientId, string password, string apiKey)
    {
        var options = new KiraOptions { ClientId = clientId, Password = password, ApiKey = apiKey };

        Should.Throw<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void DefaultEnvironmentIsSandbox()
    {
        new KiraOptions().Environment.ShouldBe(KiraEnvironment.Sandbox);
    }

    internal static KiraOptions NewOptions() => new()
    {
        ClientId = "client-id",
        Password = "password",
        ApiKey = "api-key",
        Environment = KiraEnvironment.Sandbox,
    };
}
