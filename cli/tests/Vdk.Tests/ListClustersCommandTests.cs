using System.Threading.Tasks;
using Moq;
using Vdk.Commands;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests;

public class ListClustersCommandTests : CommandTestBase
{
    [Fact]
    public async Task InvokeAsync_ShouldCallListClustersAndWriteLines()
    {
        var mockKindClient = new Mock<IKindClient>();
        mockKindClient.Setup(k => k.ListClusters()).Returns(new System.Collections.Generic.List<string> { "test-cluster" });

        var cmd = new ListClustersCommand(MockConsole.Object, mockKindClient.Object);
        await cmd.InvokeAsync();

        mockKindClient.Verify(k => k.ListClusters(), Times.Once);
        MockConsole.Verify(c => c.WriteLine("test-cluster"), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleEmptyList()
    {
        var mockKindClient = new Mock<IKindClient>();
        mockKindClient.Setup(k => k.ListClusters()).Returns(new System.Collections.Generic.List<string>());
        var cmd = new ListClustersCommand(MockConsole.Object, mockKindClient.Object);
        await cmd.InvokeAsync();
        // Should not throw, and WriteLine should not be called
        MockConsole.Verify(c => c.WriteLine(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handler_ShouldBeRegistered()
    {
        var mockKindClient = new Mock<IKindClient>();
        var cmd = new ListClustersCommand(MockConsole.Object, mockKindClient.Object);
        var invoked = false;
        mockKindClient.Setup(k => k.ListClusters()).Callback(() => invoked = true).Returns(new System.Collections.Generic.List<string>());
        await cmd.InvokeAsync();
        Assert.True(invoked);
    }
}

