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
        private readonly Func<string, IKubernetesClient> _clientFunc;
        private readonly Mock<IConsole> _consoleMock = new();
        private readonly Mock<IDockerEngine> _dockerMock = new();
        private readonly Mock<IKindClient> _kindMock = new();
        private readonly Mock<IKubernetesClient> _kubeClientMock = new();

        public ReverseProxyClientTests()
        {
            _clientFunc = _ => _kubeClientMock.Object;
        }

        [Fact]
        public void Constructor_SetsDependencies()
        {
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);
            client.Should().NotBeNull();
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
        public void Exists_ReturnsTrue_WhenDockerThrows()
        {
            _dockerMock.Setup(d => d.Exists(It.IsAny<string>(), It.IsAny<bool>())).Throws(new Exception("fail"));
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

        [Fact]
        public void PatchCoreDns_ReturnsFalse_WhenCoreDnsConfigMapNotFound()
        {
            // Arrange
            _kubeClientMock.Setup(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns(new V1Service { Metadata = new V1ObjectMeta { Name = "svc", NamespaceProperty = "ns" } });
            _kubeClientMock.Setup(x => x.Get<V1ConfigMap>("coredns", "kube-system"))
                .Returns((V1ConfigMap?)null);
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, "test-cluster");

            // Assert
            Assert.False(result);
            _consoleMock.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("CoreDNS configmap not found"))), Times.Once);
        }

        [Fact]
        public void PatchCoreDns_ReturnsFalse_WhenCorefileMissing()
        {
            // Arrange
            _kubeClientMock.Setup(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns(new V1Service { Metadata = new V1ObjectMeta { Name = "svc", NamespaceProperty = "ns" } });
            _kubeClientMock.Setup(x => x.Get<V1ConfigMap>("coredns", "kube-system"))
                .Returns(new V1ConfigMap { Data = new Dictionary<string, string>() });
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, "test-cluster");

            // Assert
            Assert.False(result);
            _consoleMock.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("CoreDNS Corefile not found"))), Times.Once);
        }

        [Fact]
        public void PatchCoreDns_ReturnsFalse_WhenIngressServiceNotFound()
        {
            // Arrange
            _kubeClientMock.SetupSequence(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns((V1Service?)null);
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, "test-cluster");

            // Assert
            Assert.False(result);
            _consoleMock.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("kgateway-system-kgateway service not found"))), Times.Once);
        }

        [Fact]
        public void PatchCoreDns_InsertsRewriteBeforeKubernetesBlock()
        {
            // Arrange: Corefile with kubernetes block but no closing brace — rewrite should still insert before the kubernetes line
            var clusterName = "test-cluster";
            var corefile = "kubernetes cluster.local in-addr.arpa ip6.arpa {";
            var configMap = new V1ConfigMap { Data = new Dictionary<string, string> { { "Corefile", corefile } } };
            var pod = new V1Pod { Metadata = new V1ObjectMeta { Name = "coredns-1" } };
            _kubeClientMock.Setup(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns(new V1Service { Metadata = new V1ObjectMeta { Name = "svc", NamespaceProperty = "ns" } });
            _kubeClientMock.Setup(x => x.Get<V1ConfigMap>("coredns", "kube-system"))
                .Returns(configMap);
            _kubeClientMock.Setup(x => x.List<V1Pod>("kube-system", It.IsAny<string>()))
                .Returns(new List<V1Pod> { pod });
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, clusterName);

            // Assert — rewrite is inserted before the kubernetes block
            Assert.True(result);
            _kubeClientMock.Verify(x => x.Update(It.Is<V1ConfigMap>(cm =>
                cm.Data["Corefile"].Contains($"rewrite name {clusterName}.dev-k8s.cloud svc.ns.svc.cluster.local"))), Times.Once);
            _kubeClientMock.Verify(x => x.Delete(pod), Times.Once);
        }

        [Fact]
        public void PatchCoreDns_ReturnsFalse_WhenNoKubernetesBlock()
        {
            // Arrange
            var corefile = "some unrelated config";
            _kubeClientMock.Setup(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns(new V1Service { Metadata = new V1ObjectMeta { Name = "svc", NamespaceProperty = "ns" } });
            _kubeClientMock.Setup(x => x.Get<V1ConfigMap>("coredns", "kube-system"))
                .Returns(new V1ConfigMap { Data = new Dictionary<string, string> { { "Corefile", corefile } } });
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, "test-cluster");

            // Assert
            Assert.False(result);
            _consoleMock.Verify(x => x.WriteError(It.Is<string>(s => s.Contains("does not contain a kubernetes block"))), Times.Once);
        }

        [Fact]
        public void PatchCoreDns_ReturnsTrue_WhenRewriteAlreadyExists()
        {
            // Arrange
            var clusterName = "test-cluster";
            var rewriteString = $"    rewrite name {clusterName}.dev-k8s.cloud svc.ns.svc.cluster.local";
            var corefile = $"kubernetes cluster.local in-addr.arpa ip6.arpa {{\n}}\n{rewriteString}\n";
            _kubeClientMock.Setup(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns(new V1Service { Metadata = new V1ObjectMeta { Name = "svc", NamespaceProperty = "ns" } });
            _kubeClientMock.Setup(x => x.Get<V1ConfigMap>("coredns", "kube-system"))
                .Returns(new V1ConfigMap { Data = new Dictionary<string, string> { { "Corefile", corefile } } });
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, clusterName);

            // Assert
            Assert.True(result);
            _consoleMock.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("already contains the rewrite entry"))), Times.Once);
        }

        [Fact]
        public void PatchCoreDns_UpdatesConfigMapAndRestartsPods()
        {
            // Arrange
            var clusterName = "test-cluster";
            var corefile = $"kubernetes cluster.local in-addr.arpa ip6.arpa {{{Environment.NewLine}}}{Environment.NewLine}";
            var configMap = new V1ConfigMap { Data = new Dictionary<string, string> { { "Corefile", corefile } } };
            var pod = new V1Pod { Metadata = new V1ObjectMeta { Name = "coredns-1" } };
            _kubeClientMock.Setup(x => x.Get<V1Service>("kgateway-system-kgateway", "kgateway-system"))
                .Returns(new V1Service { Metadata = new V1ObjectMeta { Name = "svc", NamespaceProperty = "ns" } });
            _kubeClientMock.Setup(x => x.Get<V1ConfigMap>("coredns", "kube-system"))
                .Returns(configMap);
            _kubeClientMock.Setup(x => x.List<V1Pod>("kube-system", It.IsAny<string>()))
                .Returns(new List<V1Pod> { pod });
            var client = new ReverseProxyClient(_dockerMock.Object, _clientFunc, _consoleMock.Object, _kindMock.Object);

            // Act
            var result = InvokePatchCoreDns(client, clusterName);

            // Assert
            Assert.True(result);
            _kubeClientMock.Verify(x => x.Update(It.Is<V1ConfigMap>(cm => cm.Data["Corefile"].Contains($"rewrite name {clusterName}.dev-k8s.cloud svc.ns.svc.cluster.local"))), Times.Once);
            _kubeClientMock.Verify(x => x.Delete(pod), Times.Once);
            _consoleMock.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("CoreDNS configmap updated successfully."))), Times.Once);
        }

        // Helper to invoke private PatchCoreDns via reflection
        private static bool InvokePatchCoreDns(ReverseProxyClient client, string clusterName)
        {
            var method = typeof(ReverseProxyClient).GetMethod("PatchCoreDns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)method.Invoke(client, new object[] { clusterName });
        }
    }
}