using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Abstractions;
using KubeOps.KubernetesClient;
using Moq;
using Vdk.Commands;
using Vdk.Constants;
using Vdk.Data;
using Vdk.Models;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests;

public class CreateClusterCommandTests : CommandTestBase
{
    private readonly Mock<IEmbeddedDataReader> _mockDataReader = new();
    private readonly Mock<IYamlObjectSerializer> _mockYaml = new();
    private readonly Mock<IFileSystem> _mockFileSystem = new();
    private readonly Mock<IKindClient> _mockKind = new();
    private readonly Mock<IFluxClient> _mockFlux = new();
    private readonly Mock<IReverseProxyClient> _mockReverseProxy = new();

    private CreateClusterCommand CreateCommand() =>
        new CreateClusterCommand(
            MockConsole.Object,
            _mockDataReader.Object,
            _mockYaml.Object,
            _mockFileSystem.Object,
            _mockKind.Object,
            _mockFlux.Object,
            _mockReverseProxy.Object,
            s =>  Mock.Of<IKubernetesClient>()
        );

    [Fact]
    public async Task InvokeAsync_ShouldHandleKindGetVersionThrows()
    {
        _mockKind.Setup(k => k.GetVersion()).Throws(new Exception("fail-version"));
        var cmd = CreateCommand();
        await cmd.InvokeAsync(bypassPrompt: true);
        MockConsole.Verify(c => c.WriteError(It.Is<string>(msg => msg.Contains("Unable to retrieve the installed kind version."))), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleNullKindVersion()
    {
        _mockKind.Setup(k => k.GetVersion()).Returns((string)null);
        _mockDataReader.Setup(d => d.ReadJsonObjects<KindVersionMap>(It.IsAny<string>())).Returns(new KindVersionMap());
        var cmd = CreateCommand();
        await cmd.InvokeAsync(bypassPrompt: true);
        // The implementation writes a warning, not an error, for null/empty version
        MockConsole.Verify(c => c.WriteWarning(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleCreateClusterThrows()
    {
        _mockKind.Setup(k => k.GetVersion()).Returns("v0.20.0");
        _mockKind.Setup(k => k.CreateCluster(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("fail-create"));
        _mockDataReader.Setup(d => d.ReadJsonObjects<KindVersionMap>(It.IsAny<string>())).Returns(new KindVersionMap { new KindVersion { Version = "v0.20.0", Images = new List<KubeImage> { new KubeImage { Version = "1.29.0", Image = "img" } } } });
        _mockYaml.Setup(y => y.Serialize(It.IsAny<object>())).Returns("manifest");
        _mockFileSystem.Setup(f => f.Path.GetTempFileName()).Returns("temp.yaml");
        var memStream = new System.IO.MemoryStream();
        var writer = new System.IO.StreamWriter(memStream);
        _mockFileSystem.Setup(f => f.File.CreateText("temp.yaml")).Returns(writer);
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.FullName).Returns("ConfigMounts/hosts.toml");
        _mockFileSystem.Setup(f => f.FileInfo.New("ConfigMounts/hosts.toml")).Returns(fileInfoMock.Object);
        var cmd = CreateCommand();
        await Assert.ThrowsAsync<Exception>(() => cmd.InvokeAsync("test", 1, 2, "1.29", bypassPrompt: true));
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleMasterNodeNotFound()
    {
        _mockKind.Setup(k => k.GetVersion()).Returns("v0.20.0");
        _mockKind.Setup(k => k.CreateCluster(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _mockDataReader.Setup(d => d.ReadJsonObjects<KindVersionMap>(It.IsAny<string>())).Returns(new KindVersionMap { new KindVersion { Version = "v0.20.0", Images = new List<KubeImage> { new KubeImage { Version = "1.29.0", Image = "img" } } } });
        _mockYaml.Setup(y => y.Serialize(It.IsAny<object>())).Returns("manifest");
        _mockFileSystem.Setup(f => f.Path.GetTempFileName()).Returns("temp.yaml");
        var memStream = new System.IO.MemoryStream();
        var writer = new System.IO.StreamWriter(memStream);
        _mockFileSystem.Setup(f => f.File.CreateText("temp.yaml")).Returns(writer);
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.FullName).Returns("ConfigMounts/hosts.toml");
        _mockFileSystem.Setup(f => f.FileInfo.New("ConfigMounts/hosts.toml")).Returns(fileInfoMock.Object);
        // Simulate scenario where no control-plane node with ExtraPortMappings exists (master node not found)
        // This is handled internally by the command; no need to mock _kind.GetNodes
        // Patch the test to simulate no nodes with ExtraPortMappings by passing 0 for both controlPlaneNodes and workerNodes
        var cmd = CreateCommand();
        await cmd.InvokeAsync("test", 0, 0, "1.29", bypassPrompt: true); // No nodes created
        MockConsole.Verify(c => c.WriteError(It.Is<string>(msg => msg.Contains("Unable to find the master node"))), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldHandleReverseProxyThrows()
    {
        _mockKind.Setup(k => k.GetVersion()).Returns("v0.20.0");
        _mockKind.Setup(k => k.CreateCluster(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _mockDataReader.Setup(d => d.ReadJsonObjects<KindVersionMap>(It.IsAny<string>())).Returns(new KindVersionMap { new KindVersion { Version = "v0.20.0", Images = new List<KubeImage> { new KubeImage { Version = "1.29.0", Image = "img" } } } });
        _mockYaml.Setup(y => y.Serialize(It.IsAny<object>())).Returns("manifest");
        _mockFileSystem.Setup(f => f.Path.GetTempFileName()).Returns("temp.yaml");
        var memStream = new System.IO.MemoryStream();
        var writer = new System.IO.StreamWriter(memStream);
        _mockFileSystem.Setup(f => f.File.CreateText("temp.yaml")).Returns(writer);
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.FullName).Returns("ConfigMounts/hosts.toml");
        _mockFileSystem.Setup(f => f.FileInfo.New("ConfigMounts/hosts.toml")).Returns(fileInfoMock.Object);
        // The master node is created by the command logic; no need to mock _kind.GetNodes
        // The command will create a control-plane node with ExtraPortMappings by default if controlPlaneNodes > 0
        _mockFlux.Setup(f => f.Bootstrap(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _mockReverseProxy.Setup(r => r.UpsertCluster(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("fail-proxy"));
        var cmd = CreateCommand();
        await Assert.ThrowsAsync<Exception>(() => cmd.InvokeAsync("test", 1, 2, "1.29", bypassPrompt: true));
        MockConsole.Verify(c => c.WriteLine(It.IsAny<string>()), Times.AtLeastOnce); // stack trace
        MockConsole.Verify(c => c.WriteError(It.Is<string>(msg => msg.Contains("Failed to update reverse proxy"))), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldSucceed_NormalFlow()
    {
        _mockKind.Setup(k => k.GetVersion()).Returns("v0.20.0");
        _mockKind.Setup(k => k.CreateCluster(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _mockDataReader.Setup(d => d.ReadJsonObjects<KindVersionMap>(It.IsAny<string>())).Returns(new KindVersionMap { new KindVersion { Version = "v0.20.0", Images = new List<KubeImage> { new KubeImage { Version = "1.29.0", Image = "img" } } } });
        _mockYaml.Setup(y => y.Serialize(It.IsAny<object>())).Returns("manifest");
        _mockFileSystem.Setup(f => f.Path.GetTempFileName()).Returns("temp.yaml");
        var memStream = new System.IO.MemoryStream();
        var writer = new System.IO.StreamWriter(memStream);
        _mockFileSystem.Setup(f => f.File.CreateText("temp.yaml")).Returns(writer);
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.FullName).Returns("ConfigMounts/hosts.toml");
        _mockFileSystem.Setup(f => f.FileInfo.New("ConfigMounts/hosts.toml")).Returns(fileInfoMock.Object);
        // The master node is created by the command logic; no need to mock _kind.GetNodes
        // The command will create a control-plane node with ExtraPortMappings by default if controlPlaneNodes > 0
        _mockFlux.Setup(f => f.Bootstrap(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        _mockReverseProxy.Setup(r => r.UpsertCluster(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Verifiable();
        var cmd = CreateCommand();
        await cmd.InvokeAsync("test", 1, 2, "1.29", bypassPrompt: true);
        _mockKind.VerifyAll();
        _mockFlux.VerifyAll();
        _mockReverseProxy.VerifyAll();
    }
}
