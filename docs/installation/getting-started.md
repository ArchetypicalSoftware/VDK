# Getting Started with Vega

This guide covers the installation and setup process for the Vega Development Kit (VDK) CLI and its dependencies.

## Core Prerequisites

These are required to run Vega and the KinD clusters it creates:

- **[Docker](https://docs.docker.com/get-docker/)**: Vega uses KinD (Kubernetes in Docker), so a working Docker installation (Docker Desktop or Docker Engine) is essential.
- **[kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)**: The Kubernetes command-line tool, used to interact with your clusters.
- **[KinD](https://kind.sigs.k8s.io/docs/user/quick-start/#installation)**: Kubernetes in Docker, used to create local clusters.

## Quick Start with Devbox

The easiest way to get started is using [Devbox](https://www.jetpack.io/devbox/docs/):

1. **Install Devbox:**

   ```bash
   curl -fsSL https://get.jetify.com/devbox | bash
   ```

2. **Clone the repository:**

   ```bash
   git clone https://github.com/ArchetypicalSoftware/VDK.git
   cd VDK
   ```

3. **Start a Devbox shell:**

   ```bash
   devbox shell
   ```

4. **Initialize Vega:**

   ```bash
   vega init
   ```

This sets up the complete development environment including the registry, reverse proxy, and a default cluster.

## Manual Installation

If not using Devbox, ensure you have:

- .NET 10 SDK
- Docker
- KinD
- kubectl
- Flux CLI

Then build from source:

```bash
cd cli/src/Vdk
dotnet build
dotnet run -- --help
```

## Environment Configuration

### GitHub Token (Optional)

For Flux GitOps integration, you may need a GitHub Personal Access Token:

```bash
export GITHUB_VDK_TOKEN="<YOUR_GHPAT>"
```

Add this to your shell profile (`.bashrc`, `.zshrc`, etc.) for persistence.

### Reverse Proxy Port

If port 443 is already in use on your system:

```bash
export REVERSE_PROXY_HOST_PORT=8443
```

## macOS Sequoia/Corporate Proxy Certificate Issue

If you encounter SSL errors behind a corporate proxy (like Netskope), add your corporate certificates to Nix's trust store:

1. Generate a combined certificate bundle:

   ```bash
   security export -t certs -f pemseq -k /Library/Keychains/System.keychain -o /tmp/certs-system.pem
   security export -t certs -f pemseq -k /System/Library/Keychains/SystemRootCertificates.keychain -o /tmp/certs-root.pem
   cat /tmp/certs-root.pem /tmp/certs-system.pem > /tmp/ca_cert.pem
   sudo mv /tmp/ca_cert.pem /etc/nix/
   ```

2. Update `/etc/nix/nix.conf`:

   ```ini
   ssl-cert-file = /etc/nix/ca_cert.pem
   ```

3. Restart the Nix daemon:

   ```bash
   sudo launchctl unload /Library/LaunchDaemons/org.nixos.nix-daemon.plist
   sudo launchctl load /Library/LaunchDaemons/org.nixos.nix-daemon.plist
   ```

## Verifying Installation

Once Vega is installed, verify it:

```bash
vega --version
```

List available commands:

```bash
vega --help
```

## Next Steps

Now that Vega is set up, learn how to [Create Clusters](../usage/creating-clusters.md).
