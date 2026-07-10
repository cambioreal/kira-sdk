using CambioReal.Kira.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CambioReal.Kira.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void ResolvesTheClientFromTheContainer()
    {
        using var provider = Build(options =>
        {
            options.ClientId = "cid";
            options.Password = "pwd";
            options.ApiKey = "key";
        });

        var client = provider.GetRequiredService<KiraClient>();

        client.Users.ShouldNotBeNull();
        client.VirtualAccounts.ShouldNotBeNull();
        client.Platform.ShouldNotBeNull();
    }

    /// <summary>O provedor de token guarda o JWT em cache; um por container, não um por requisição.</summary>
    [Fact]
    public void TokenProviderIsASingleton()
    {
        using var provider = Build(ValidOptions);

        provider.GetRequiredService<IKiraTokenProvider>()
            .ShouldBeSameAs(provider.GetRequiredService<IKiraTokenProvider>());
    }

    [Fact]
    public void BindsOptionsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientId"] = "cid-from-config",
                ["Password"] = "pwd",
                ["ApiKey"] = "key",
                ["Environment"] = "Production",
                ["Timeout"] = "00:00:45",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddKiraClient(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<KiraOptions>>().Value;

        options.ClientId.ShouldBe("cid-from-config");
        options.Environment.ShouldBe(KiraEnvironment.Production);
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(45));
        options.ResolveBaseAddress().ToString().ShouldBe("https://api.balampay.com/");
    }

    [Fact]
    public void MissingCredentialsFailWhenTheClientIsResolved()
    {
        using var provider = Build(options => options.ClientId = "cid"); // sem Password nem ApiKey

        Should.Throw<Exception>(provider.GetRequiredService<KiraClient>)
            .ShouldNotBeNull();
    }

    [Fact]
    public void BaseAddressComesFromTheConfiguredEnvironment()
    {
        using var provider = Build(options =>
        {
            ValidOptions(options);
            options.Environment = KiraEnvironment.Sandbox;
        });

        var factory = provider.GetRequiredService<IHttpClientFactory>();

        using var client = factory.CreateClient("kira.auth");
        client.BaseAddress!.ToString().ShouldBe("https://api.balampay.com/sandbox/");
    }

    private static void ValidOptions(KiraOptions options)
    {
        options.ClientId = "cid";
        options.Password = "pwd";
        options.ApiKey = "key";
    }

    private static ServiceProvider Build(Action<KiraOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddKiraClient(configure);
        return services.BuildServiceProvider();
    }
}
