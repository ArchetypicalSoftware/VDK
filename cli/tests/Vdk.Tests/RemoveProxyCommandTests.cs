using System.Threading.Tasks;
using Moq;
using Vdk.Commands;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests;

public class RemoveProxyCommandTests : CommandTestBase
{
    [Fact]
    public async Task InvokeAsync_ShouldCallDelete()
    {
        var mockProxyClient = new Mock<IReverseProxyClient>();
        var cmd = new RemoveProxyCommand(MockConsole.Object, mockProxyClient.Object);
        await cmd.InvokeAsync();
        mockProxyClient.Verify(p => p.Delete(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleExceptionAndWriteError()
    {
        var mockProxyClient = new Mock<IReverseProxyClient>();
        mockProxyClient.Setup(p => p.Delete()).Throws(new System.Exception("fail!"));
        var cmd = new RemoveProxyCommand(MockConsole.Object, mockProxyClient.Object);
        await cmd.InvokeAsync();
        MockConsole.Verify(c => c.WriteError(It.Is<string>(msg => msg.Contains("Error removing proxy")), "fail!"), Times.Once);
    }

    [Fact]
    public async Task Handler_ShouldBeRegistered()
    {
        var mockProxyClient = new Mock<IReverseProxyClient>();
        var cmd = new RemoveProxyCommand(MockConsole.Object, mockProxyClient.Object);
        var invoked = false;
        mockProxyClient.Setup(p => p.Delete()).Callback(() => invoked = true);
        await cmd.InvokeAsync();
        Assert.True(invoked);
    }
}

