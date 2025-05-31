using System;
using System.IO;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Vdk.Services;
using Xunit;
using k8s.Models;
using KubeOps.KubernetesClient;

namespace Vdk.Tests
{
    public class ReverseProxyClientTests
    {
        private readonly Mock<IDockerEngine> _dockerMock = new();
        private readonly Mock<IConsole> _consoleMock = new();
        private readonly Mock<IKindClient> _kindMock = new();
        private readonly Mock<IKubernetesClient> _k8sMock = new();
        private readonly Func<string, IKubernetesClient> _clientFunc;

        public ReverseProxyClientTests()
        {
            _clientFunc = _ => _k8sMock.Object;
        }

        [Fact]
        public void Constructor_SetsDependencies()
        {
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);
            client.Should().NotBeNull();
        }

        [Fact]
        public void Exists_ReturnsTrue_WhenDockerThrows()
        {
            _dockerMock.Setup(d => d.Exists(It.IsAny<string>(), It.IsAny<bool>())).Throws(new Exception("fail"));
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);
            client.Exists().Should().BeTrue();
        }

        [Fact]
        public void Exists_ReturnsFalse_WhenDockerReturnsFalse()
        {
            _dockerMock.Setup(d => d.Exists(It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);
            client.Exists().Should().BeFalse();
        }

        [Fact]
        public void Exists_ReturnsTrue_WhenDockerReturnsTrue()
        {
            _dockerMock.Setup(d => d.Exists(It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);
            client.Exists().Should().BeTrue();
        }

        [Fact]
        public void InitConfFile_CreatesFileAndWritesConfig()
        {
            var tempFile = Path.GetTempFileName();
            var file = new FileInfo(tempFile);
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);
            client.GetType().GetMethod("InitConfFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(client, new object[] { file });
            File.Exists(tempFile).Should().BeTrue();
            File.ReadAllText(tempFile).Should().Contain("server {");
            File.Delete(tempFile);
        }
    }
}