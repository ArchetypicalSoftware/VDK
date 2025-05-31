using System.Threading.Tasks;
using Moq;
using Vdk.Commands;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests;

public class RemoveRegistryCommandTests : CommandTestBase
{
    [Fact]
    public async Task InvokeAsync_ShouldCallDestroy()
    {
        var mockHubClient = new Mock<IHubClient>();
        var cmd = new RemoveRegistryCommand(MockConsole.Object, mockHubClient.Object);
        await cmd.InvokeAsync();
        mockHubClient.Verify(h => h.Destroy(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleExceptionAndWriteError()
    {
        var mockHubClient = new Mock<IHubClient>();
        mockHubClient.Setup(h => h.Destroy()).Throws(new System.Exception("fail!"));
        var cmd = new RemoveRegistryCommand(MockConsole.Object, mockHubClient.Object);
        await cmd.InvokeAsync();
        MockConsole.Verify(c => c.WriteError(It.Is<string>(msg => msg.Contains("Error removing registry")), "fail!"), Times.Once);
    }

    [Fact]
    public async Task Handler_ShouldBeRegistered()
    {
        var mockHubClient = new Mock<IHubClient>();
        var cmd = new RemoveRegistryCommand(MockConsole.Object, mockHubClient.Object);
        var invoked = false;
        mockHubClient.Setup(h => h.Destroy()).Callback(() => invoked = true);
        await cmd.InvokeAsync();
        Assert.True(invoked);
    }
}

