# Vega Development Kit (VDK)

**Develop Kubernetes applications locally with ease using KinD (Kubernetes in Docker).**

VDK is a Command Line Interface (CLI) tool designed to help developers quickly and securely set up local Kubernetes clusters using [KinD](https://kind.sigs.k8s.io/). It simplifies the process of creating clusters with varying configurations (versions, node counts) to assist in building and testing applications for platforms like Vega.

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE) <!-- Add license badge if applicable -->
<!-- Add build status, release badges etc. later -->

## Overview

Building and testing applications designed for Kubernetes often requires a realistic cluster environment. VDK streamlines the setup of local clusters via KinD, providing commands to:

*   Create single or multi-node Kubernetes clusters.
*   Specify desired Kubernetes versions.
*   Manage the lifecycle of these local clusters (list, delete).
*   Easily integrate with `kubectl`.

This tool is particularly useful for developers working on the Vega platform, enabling consistent and reproducible development environments.

## Installation

For detailed installation instructions for your operating system, please refer to the **[Getting Started Guide](./docs/installation/getting-started.md)**.

**Prerequisites:**

*   [Docker](https://docs.docker.com/get-docker/)
*   [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) (Recommended for interacting with clusters)

*For development prerequisites (Devbox/Nix), see the [Getting Started Guide](./docs/installation/getting-started.md).*

## Quick Start

1.  **Login (device code flow):**
    ```bash
    vega login
    ```
    Follow the printed link/code to authenticate. Tokens are stored under `~/.vega/tokens`.

2.  **Create a default cluster:**
    ```bash
    vega create cluster
    ```
    *(This may take a few minutes)*

3.  **Verify cluster access:**
    ```bash
    kubectl cluster-info --context kind-kind
    ```

4.  **Delete the cluster:**
    ```bash
    vega delete cluster
    ```

## Usage

For comprehensive usage details, examples, and command references, please see the **Usage Documentation**:

*   **[Creating Clusters](./docs/usage/creating-clusters.md)**
*   **[Managing Clusters](./docs/usage/managing-clusters.md)**
*   **[Command Reference](./docs/usage/command-reference.md)**

## Authentication

VDK enforces that you are logged in before executing most `vega` commands. Authentication uses an OAuth2 Device Code flow (Ory Hydra):

* __Login__: `vega login [--profile <name>]`
* __Logout__: `vega logout [--profile <name>]`
* __Multi-profile__: Use `--profile` to login or logout different affiliations. The current profile pointer is stored in `~/.vega/tokens/.current_profile`.
* __Token storage__: Access/refresh tokens are stored per-profile in `~/.vega/tokens/<profile>.json`. Refresh is automatic when the access token expires.

During cluster creation, VDK extracts `TenantId` from your access token and writes a ConfigMap named `vega-tenant` in the `vega-system` namespace so downstream tooling can correlate ownership.

## Contributing

We welcome contributions! Please read our **[Contribution Guidelines](./docs/contribution/guidelines.md)** and **[Development Setup](./docs/contribution/development-setup.md)** guides to get started.

## Debugging & Troubleshooting

If you encounter issues while using VDK, consult the **[Troubleshooting Guide](./docs/debugging/troubleshooting.md)**.

## License

This project is licensed under the Apache 2.0 License - see the [LICENSE](LICENSE) file for details. *(Ensure a LICENSE file exists)*

## Getting Started

- Install DevBox

```
    curl -fsSL https://get.jetify.com/devbox | bash
```
- Clone the repository `git clone https://github.com/ArchetypicalSoftware/VDK.git`
- Start DevBox session in the cloned repository
```
    devbox shell
```
- Run `vega --help` to see the available commands

### Configuration options

If you already have a process listening on your host on port 443, you will have issues with the vega reverse proxy.
You can either stop the existing process or change the port by defining the environment variable `REVERSE_PROXY_HOST_PORT`. 
The default port is 443.
