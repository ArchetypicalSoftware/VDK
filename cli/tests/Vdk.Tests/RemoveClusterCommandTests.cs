using System.Threading.Tasks;
using Moq;
using Vdk.Commands;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests;

public class RemoveClusterCommandTests : CommandTestBase
{
    [Fact]
    public async Task InvokeAsync_ShouldCallDeleteCluster()
    {
        var mockKindClient = new Mock<IKindClient>();
        var cmd = new RemoveClusterCommand(MockConsole.Object, mockKindClient.Object);
        await cmd.InvokeAsync("test-cluster");
        mockKindClient.Verify(k => k.DeleteCluster("test-cluster"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldWriteError_WhenDeleteClusterThrows()
    {
        var mockKindClient = new Mock<IKindClient>();
        mockKindClient.Setup(k => k.DeleteCluster(It.IsAny<string>())).Throws(new System.Exception("fail!"));
        var cmd = new RemoveClusterCommand(MockConsole.Object, mockKindClient.Object);
        await cmd.InvokeAsync("test-cluster");
        MockConsole.Verify(c => c.WriteError(It.Is<string>(msg => msg.Contains("Error removing cluster '{ClusterName}': {ErrorMessage}")), "test-cluster", "fail!"), Times.Once);
    }

    [Fact]
    public async Task Handler_ShouldBeRegistered()
    {
        var mockKindClient = new Mock<IKindClient>();
        var cmd = new RemoveClusterCommand(MockConsole.Object, mockKindClient.Object);
        var invoked = false;
        mockKindClient.Setup(k => k.DeleteCluster(It.IsAny<string>())).Callback(() => invoked = true);
        await cmd.InvokeAsync("test-cluster");
        Assert.True(invoked);
    }
}

