using System;
using System.Collections.Generic;
using Moq;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests;

public class KindClientTests
{

    [Fact]
    public void CreateCluster_ShouldCallShellWithCorrectArgs()
    {
        var mockConsole = new Mock<IConsole>();
        var mockShell = new Mock<IShell>();
        var client = new KindClient(mockConsole.Object, mockShell.Object);
        mockShell.Setup(s => s.Execute("kind", It.Is<string[]>(args => args[0] == "create" && args[1] == "cluster"))).Verifiable();
        client.CreateCluster("test", "config.yaml");
        mockShell.VerifyAll();
    }

    [Fact]
    public void DeleteCluster_ShouldCallShellWithCorrectArgs()
    {
        var mockConsole = new Mock<IConsole>();
        var mockShell = new Mock<IShell>();
        var client = new KindClient(mockConsole.Object, mockShell.Object);
        mockShell.Setup(s => s.Execute("kind", It.Is<string[]>(args => args[0] == "delete" && args[1] == "cluster"))).Verifiable();
        client.DeleteCluster("test");
        mockShell.VerifyAll();
    }

    [Fact]
    public void ListClusters_ShouldReturnParsedList()
    {
        var mockConsole = new Mock<IConsole>();
        var mockShell = new Mock<IShell>();
        var client = new KindClient(mockConsole.Object, mockShell.Object);
        mockShell.Setup(s => s.ExecuteAndCapture("kind", It.Is<string[]>(a => a != null && a.SequenceEqual(new[] { "get", "clusters" }))))
            .Callback((string command, string[] args) =>
            {
                System.Diagnostics.Debug.WriteLine($"MOCK HIT: command={command}, args=[{string.Join(",", args)}]");
            })
            .Returns($"test1{Environment.NewLine}test2{Environment.NewLine}");
        var clusters = client.ListClusters();
        Assert.Equal(new List<string> { "test1", "test2" }, clusters);
    }

    [Fact]
    public void ListClusters_ShouldReturnEmptyListOnEmptyOutput()
    {
        var mockConsole = new Mock<IConsole>();
        var mockShell = new Mock<IShell>();
        var client = new KindClient(mockConsole.Object, mockShell.Object);
        mockShell.Setup(s => s.ExecuteAndCapture("kind", It.Is<string[]>(a => a != null && a.SequenceEqual(new[] { "get", "clusters" }))))
            .Callback((string command, string[] args) =>
            {
                System.Diagnostics.Debug.WriteLine($"MOCK HIT: command={command}, args=[{string.Join(",", args)}]");
            })
            .Returns("");
        var clusters = client.ListClusters();
        Assert.Empty(clusters);
    }

    [Fact]
    public void GetVersion_ShouldReturnParsedVersion()
    {
        var mockConsole = new Mock<IConsole>();
        var mockShell = new Mock<IShell>();
        var client = new KindClient(mockConsole.Object, mockShell.Object);
        mockShell.Setup(s => s.ExecuteAndCapture("kind", It.Is<string[]>(a => a != null && a.SequenceEqual(new[] { "--version" }))))
            .Callback((string command, string[] args) =>
            {
                System.Diagnostics.Debug.WriteLine($"MOCK HIT: command={command}, args=[{string.Join(",", args)}]");
            })
            .Returns("kind version 0.27.0");
        var version = client.GetVersion();
        System.Diagnostics.Debug.WriteLine($"Actual version: {version}");
        Assert.Equal("0.27.0", version);
    }

    [Fact]
    public void GetVersion_ShouldReturnNullOnShortString()
    {
        var mockConsole = new Mock<IConsole>();
        var mockShell = new Mock<IShell>();
        var client = new KindClient(mockConsole.Object, mockShell.Object);
        mockShell.Setup(s => s.ExecuteAndCapture("kind", It.Is<string[]>(a => a != null && a.SequenceEqual(new[] { "--version" }))))
            .Callback((string command, string[] args) =>
            {
                System.Diagnostics.Debug.WriteLine($"MOCK HIT: command={command}, args=[{string.Join(",", args)}]");
            })
            .Returns("short");
        var version = client.GetVersion();
        System.Diagnostics.Debug.WriteLine($"Actual version (short): {version}");
        Assert.Null(version);
    }
}
