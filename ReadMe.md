# Vega Development Kit (VDK)

**Develop Kubernetes applications locally with ease using KinD (Kubernetes in Docker).**

VDK is a Command Line Interface (CLI) tool designed to help developers quickly and securely set up local Kubernetes clusters using [KinD](https://kind.sigs.k8s.io/). It simplifies the process of creating clusters with varying configurations (versions, node counts) to assist in building and testing applications for platforms like Vega.

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)

## Overview

Building and testing applications designed for Kubernetes often requires a realistic cluster environment. VDK streamlines the setup of local clusters via KinD, providing:

- Single or multi-node Kubernetes clusters
- Configurable Kubernetes versions
- Local container registry (Zot) for image storage
- TLS-enabled reverse proxy (nginx) for secure access
- GitOps with Flux CD
- Easy cluster lifecycle management

This tool is particularly useful for developers working on the Vega platform, enabling consistent and reproducible development environments.

## Quick Start

1. **Install Devbox:**

   ```bash
   curl -fsSL https://get.jetify.com/devbox | bash
   ```

2. **Clone and enter the repository:**

   ```bash
   git clone https://github.com/ArchetypicalSoftware/VDK.git
   cd VDK
   devbox shell
   ```

3. **Initialize the environment:**

   ```bash
   vega init
   ```

   This creates the registry, reverse proxy, and a default cluster.

4. **Verify cluster access:**

   ```bash
   kubectl cluster-info --context kind-vdk
   ```

5. **When done, remove the cluster:**

   ```bash
   vega remove cluster
   ```

## Configuration

If port 443 is already in use, set a different port for the reverse proxy:

```bash
export REVERSE_PROXY_HOST_PORT=8443
```

## Documentation

| Topic | Description |
|-------|-------------|
| [Getting Started](./docs/installation/getting-started.md) | Installation and setup |
| [Creating Clusters](./docs/usage/creating-clusters.md) | How to create clusters |
| [Managing Clusters](./docs/usage/managing-clusters.md) | Cluster lifecycle management |
| [Command Reference](./docs/usage/command-reference.md) | Complete command documentation |
| [Troubleshooting](./docs/debugging/troubleshooting.md) | Common issues and solutions |

## Contributing

We welcome contributions! Please read our guides:

- [Contribution Guidelines](./docs/contribution/guidelines.md)
- [Development Setup](./docs/contribution/development-setup.md)

## License

This project is licensed under the Apache 2.0 License - see the [LICENSE](LICENSE) file for details.
