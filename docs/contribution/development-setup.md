# Development Setup

Setting up your environment to contribute to Vega.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/)
- [Git](https://git-scm.com/)
- [KinD](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) (Kubernetes in Docker)
- [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)

### Recommended: Using Devbox

For a reproducible development environment, use [Devbox](https://www.jetpack.io/devbox/docs/):

1. Install Devbox:

   ```bash
   curl -fsSL https://get.jetify.com/devbox | bash
   ```

2. Start a Devbox shell in the repository:

   ```bash
   devbox shell
   ```

This automatically sets up all required dependencies.

## Building from Source

1. Clone the repository (or your fork):

   ```bash
   git clone https://github.com/ArchetypicalSoftware/VDK.git
   cd VDK
   ```

2. Navigate to the CLI project:

   ```bash
   cd cli/src/Vdk
   ```

3. Build the project:

   ```bash
   dotnet build
   ```

4. Run the CLI:

   ```bash
   dotnet run -- --help
   ```

## Running Tests

From the repository root:

```bash
cd cli
dotnet test
```

Or run specific tests:

```bash
dotnet test --filter "FullyQualifiedName~YourTestName"
```

## Publishing

To create a self-contained executable:

```bash
cd cli/src/Vdk
dotnet publish -c Release
```

The output will be in `bin/Release/net10.0/publish/`.

## Project Structure

```
VDK/
├── cli/
│   └── src/
│       └── Vdk/
│           ├── Commands/       # CLI command implementations
│           ├── Services/       # Business logic and integrations
│           ├── Models/         # Data models
│           ├── Constants/      # Configuration constants
│           ├── Certs/          # TLS certificates
│           └── ConfigMounts/   # Container configuration files
└── docs/                       # Documentation
```

## Key Technologies

- **System.CommandLine**: CLI framework
- **KubeOps.KubernetesClient**: Kubernetes API client
- **Docker.DotNet**: Docker API client
- **YamlDotNet**: YAML serialization
- **Flux CD**: GitOps toolkit
