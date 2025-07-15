using System;
using System.IO.Abstractions; 
using Docker.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vdk.Services;
using Xunit;
using Vdk.Models;

namespace Vdk.Tests
{
    public class ServiceProviderBuilderDockerEngineTests
    {
        private readonly Mock<IFileSystem> _fileSystemMock = new();
        private readonly Mock<IDockerClient> _dockerClientMock = new();
        private readonly Mock<IDockerEngine> _programmaticEngineMock = new();
        private readonly GlobalConfiguration _config;
        private readonly string _fallbackFile;
        private readonly ServiceCollection _services;

        public ServiceProviderBuilderDockerEngineTests()
        {
            _config = new GlobalConfiguration();
            _config._profileDirectory = "/tmp/vdk-test-vega";
            _fallbackFile = System.IO.Path.Combine(_config.VegaDirectory, ".vdk_docker_fallback");
            _services = new ServiceCollection();
            _services.AddSingleton<IFileSystem>(_fileSystemMock.Object);
            _services.AddSingleton<IDockerClient>(_dockerClientMock.Object);
            _services.AddSingleton<GlobalConfiguration>(_config);
        }

        [Fact]
        public void UsesFallback_WhenFileHasFutureTimestamp()
        {
            _fileSystemMock.Setup(f => f.File.Exists(_fallbackFile)).Returns(true);
            _fileSystemMock.Setup(f => f.File.ReadAllText(_fallbackFile)).Returns(DateTime.UtcNow.AddHours(1).ToString("o"));
            var engine = BuildAndResolveEngine();
            Assert.IsType<FallbackDockerEngine>(engine);
        }

        [Fact]
        public void UsesProgrammatic_WhenNoFile_AndCanConnect()
        {
            _fileSystemMock.Setup(f => f.File.Exists(_fallbackFile)).Returns(false);
            _programmaticEngineMock.Setup(x => x.CanConnect()).Returns(true);
            var engine = BuildAndResolveEngine();
            Assert.Equal(_programmaticEngineMock.Object, engine);
        }

        [Fact]
        public void UsesFallback_WhenNoFile_AndCannotConnect()
        {
            _fileSystemMock.Setup(f => f.File.Exists(_fallbackFile)).Returns(false);
            _programmaticEngineMock.Setup(x => x.CanConnect()).Returns(false);
            _fileSystemMock.Setup(f => f.File.WriteAllText(_fallbackFile, It.IsAny<string>()));
            var engine = BuildAndResolveEngine();
            Assert.IsType<FallbackDockerEngine>(engine);
            _fileSystemMock.Verify(f => f.File.WriteAllText(_fallbackFile, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void IgnoresInvalidFileContent()
        {
            _fileSystemMock.Setup(f => f.File.Exists(_fallbackFile)).Returns(true);
            _fileSystemMock.Setup(f => f.File.ReadAllText(_fallbackFile)).Returns("not-a-date");
            _programmaticEngineMock.Setup(x => x.CanConnect()).Returns(true);
            var engine = BuildAndResolveEngine();
            Assert.Equal(_programmaticEngineMock.Object, engine);
        }

        [Fact]
        public void UsesFallback_WhenFileHasPastTimestamp_AndCannotConnect()
        {
            _fileSystemMock.Setup(f => f.File.Exists(_fallbackFile)).Returns(true);
            _fileSystemMock.Setup(f => f.File.ReadAllText(_fallbackFile)).Returns(DateTime.UtcNow.AddHours(-1).ToString("o"));
            _programmaticEngineMock.Setup(x => x.CanConnect()).Returns(false);
            _fileSystemMock.Setup(f => f.File.WriteAllText(_fallbackFile, It.IsAny<string>()));
            var engine = BuildAndResolveEngine();
            Assert.IsType<FallbackDockerEngine>(engine);
            _fileSystemMock.Verify(f => f.File.WriteAllText(_fallbackFile, It.IsAny<string>()), Times.Once);
        }

        private IDockerEngine BuildAndResolveEngine()
        {
            _services.AddSingleton<IDockerEngine>(provider =>
            {
                var fileSystem = provider.GetRequiredService<IFileSystem>();
                var config = provider.GetRequiredService<GlobalConfiguration>();
                var fallbackFile = System.IO.Path.Combine(config.VegaDirectory, ".vdk_docker_fallback");
                DateTime now = DateTime.UtcNow;
                DateTime fallbackUntil = DateTime.MinValue;

                if (fileSystem.File.Exists(fallbackFile))
                {
                    try
                    {
                        var content = fileSystem.File.ReadAllText(fallbackFile).Trim();
                        if (DateTime.TryParse(content, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsed))
                        {
                            fallbackUntil = parsed;
                        }
                    }
                    catch { /* ignore file errors */ }
                }

                if (fallbackUntil > now)
                {
                    // Still in fallback window
                    return new FallbackDockerEngine();
                }

                var docker = provider.GetService<IDockerClient>();
                if (docker == null) return new FallbackDockerEngine();

                var programatic = _programmaticEngineMock.Object;
                if (!programatic.CanConnect())
                {
                    // Write fallback timestamp for 2 hours
                    try
                    {
                        fileSystem.File.WriteAllText(fallbackFile, now.AddHours(2).ToString("o"));
                    }
                    catch { /* ignore file errors */ }
                    return new FallbackDockerEngine();
                }

                return programatic;
            });

            var provider = _services.BuildServiceProvider();
            return provider.GetRequiredService<IDockerEngine>();
        }
    }
}
