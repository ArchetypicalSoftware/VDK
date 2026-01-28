using Vdk.Models;
using Vdk.Services;
using Xunit;

namespace Vdk.Tests
{
    public class FallbackDockerEngineTests : IDisposable
    {
        private readonly FallbackDockerEngine _engine = new();
        private readonly string _containerName = $"vdk_test_{Guid.NewGuid().ToString().Substring(0, 8)}";
        private readonly string _image = "alpine";
        private readonly bool _dockerAvailable;

        public FallbackDockerEngineTests()
        {
            // Check if Docker is available before running tests
            _dockerAvailable = _engine.CanConnect();
            if (_dockerAvailable)
            {
                // Pull image to avoid network flakiness in tests
                FallbackDockerEngine.RunProcess("docker", $"pull {_image}", out _, out _);
            }
        }

        [SkippableFact]
        public void Run_And_Exists_And_Delete_Works()
        {
            Skip.IfNot(_dockerAvailable, "Docker is not available on this machine");

            var result = _engine.Run(_image, _containerName, null, null, null, new[] { "sleep", "60" });
            Assert.True(result, "Container should start");
            Assert.True(_engine.Exists(_containerName), "Container should exist and be running");
            Assert.True(_engine.Delete(_containerName), "Container should be deleted");
            Assert.False(_engine.Exists(_containerName, false), "Container should not exist after delete");
        }

        [SkippableFact]
        public void Stop_And_Exec_Works()
        {
            Skip.IfNot(_dockerAvailable, "Docker is not available on this machine");

            var started = _engine.Run(_image, _containerName, null, null, null, new[] { "sleep", "60" });
            Assert.True(started, "Container should start");
            // Exec a command
            Assert.True(_engine.Exec(_containerName, new[] { "sh", "-c", "echo hello" }), "Exec should succeed");
            // Stop
            Assert.True(_engine.Stop(_containerName), "Stop should succeed");
            // After stop, Exists with checkRunning=true should be false
            Assert.False(_engine.Exists(_containerName, true), "Container should not be running after stop");
            // Delete
            Assert.True(_engine.Delete(_containerName), "Container should be deleted");
        }

        [SkippableFact]
        public void Run_With_Ports_And_Volumes_Works()
        {
            Skip.IfNot(_dockerAvailable, "Docker is not available on this machine");

            var ports = new[] { new PortMapping { HostPort = 12345, ContainerPort = 80 } };
            var volumes = new[] { new FileMapping { Source = "/tmp", Destination = "/mnt" } };
            var result = _engine.Run(_image, _containerName, ports, null, volumes, new[] { "sleep", "60" });
            Assert.True(result, "Container should start with ports and volumes");
            Assert.True(_engine.Exists(_containerName), "Container should exist");
            Assert.True(_engine.Delete(_containerName), "Container should be deleted");
        }

        public void Dispose()
        {
            // Cleanup in case test fails
            _engine.Delete(_containerName);
        }
    }
}
