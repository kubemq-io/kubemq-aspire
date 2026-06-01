using KubeMQ.Aspire.Client;
using Xunit;

namespace KubeMQ.Aspire.Client.Tests;

public sealed class KubeMQConfigurationExceptionTests
{
    [Fact]
    public void Constructor_MessageOnly_SetsMessage()
    {
        var ex = new KubeMQConfigurationException("bad config");

        Assert.Equal("bad config", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructor_MessageAndInner_SetsBoth()
    {
        var inner = new InvalidOperationException("root cause");
        var ex = new KubeMQConfigurationException("bad config", inner);

        Assert.Equal("bad config", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
