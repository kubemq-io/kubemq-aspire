using KubeMQ.Aspire.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class ConnectionStringParserTests
{
    private static HostApplicationBuilder CreateBuilderWith(string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:test"] = connectionString,
        });
        return builder;
    }

    [Theory]
    [InlineData("localhost:50000")]
    [InlineData("192.168.1.1:50000")]
    [InlineData("[::1]:50000")]
    [InlineData("kubemq.kubemq-ns.svc.cluster.local:50000")]
    [InlineData("my-host:1")]
    [InlineData("my-host:65535")]
    public void Parse_ValidFormats_Succeeds(string connectionString)
    {
        var builder = CreateBuilderWith(connectionString);

        var exception = Record.Exception(() => builder.AddKubeMQClient("test"));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("localhost", "host:port")]
    [InlineData("localhost:0", "between 1 and 65535")]
    [InlineData("localhost:99999", "between 1 and 65535")]
    [InlineData("localhost:abc", "between 1 and 65535")]
    [InlineData(":50000", "empty host")]
    [InlineData("[::1:50000", "IPv6")]
    public void Parse_InvalidFormats_ThrowsConfigException(
        string connectionString,
        string expectedMessagePart)
    {
        var builder = CreateBuilderWith(connectionString);

        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));

        Assert.Contains(expectedMessagePart, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespace_ThrowsConfigException(string? connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        if (connectionString is not null)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:test"] = connectionString,
            });
        }

        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));

        Assert.Contains("missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://localhost:50000")]
    [InlineData("grpc://localhost:50000")]
    [InlineData("https://localhost:50000")]
    public void Parse_SchemePrefix_ThrowsConfigException(string connectionString)
    {
        var builder = CreateBuilderWith(connectionString);

        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));

        Assert.Contains("scheme prefix", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parser_SchemePrefix_ErrorDoesNotEchoInput()
    {
        var builder = CreateBuilderWith("https://my-secret-host:50000");
        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));
        Assert.DoesNotContain("my-secret-host", ex.Message);
    }

    [Fact]
    public void Parser_InvalidFormat_ErrorDoesNotEchoInput()
    {
        var builder = CreateBuilderWith("my-secret-host");
        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));
        Assert.DoesNotContain("my-secret-host", ex.Message);
    }

    [Theory]
    [InlineData(":50000")]
    [InlineData("[]:50000")]
    public void Parser_EmptyHost_Throws(string connectionString)
    {
        var builder = CreateBuilderWith(connectionString);
        Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));
    }

    [Fact]
    public void Parser_NegativePort_Throws()
    {
        var builder = CreateBuilderWith("host:-1");
        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));
        Assert.Contains("between 1 and 65535", ex.Message);
    }

    [Fact]
    public void Parser_BareIPv6_Throws()
    {
        var builder = CreateBuilderWith("::1:50000");
        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));
        Assert.Contains("IPv6", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("brackets", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(" localhost:50000 ")]
    [InlineData("  192.168.1.1:50000  ")]
    public void Parser_WhitespacePaddedInput_TrimsAndSucceeds(string connectionString)
    {
        var builder = CreateBuilderWith(connectionString);
        var exception = Record.Exception(() => builder.AddKubeMQClient("test"));
        Assert.Null(exception);
    }

    [Fact]
    public void Parser_WhitespaceOnlyHost_Throws()
    {
        var builder = CreateBuilderWith("  :50000");
        var ex = Assert.Throws<KubeMQConfigurationException>(
            () => builder.AddKubeMQClient("test"));
        Assert.Contains("empty host", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
