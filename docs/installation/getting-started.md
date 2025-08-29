# Getting Started with VDK

This guide covers the installation and setup process for the Vega Development Kit (VDK) CLI and its dependencies.

## Core Prerequisites

These are required to run VDK and the KinD clusters it creates:

*   **[Docker](https://docs.docker.com/get-docker/)**: VDK uses KinD (Kubernetes in Docker), so a working Docker installation (Docker Desktop or Docker Engine) is essential.
*   **[kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)**: The Kubernetes command-line tool, used to interact with your clusters.

## Development Environment Prerequisites (Using Devbox/Nix)

For contributing to VDK development or running it from source, we use [Devbox](https://www.jetpack.io/devbox/docs/) which leverages [Nix](https://nixos.org/download.html) for reproducible environments.

1.  **Install Nix:** Follow the official [Nix installation guide](https://nixos.org/download.html).

2.  **Install Devbox:** Follow the official [Devbox installation guide](https://www.jetpack.io/devbox/docs/installing_devbox/).

3.  **Environment Setup (One-time):**
    *   **GITHUB_VDK_TOKEN:** You might need a GitHub Personal Access Token (PAT) for accessing certain resources during development or build processes. Add it to your shell configuration (e.g., `.bashrc`, `.zshrc`):
        ```bash
        echo 'export GITHUB_VDK_TOKEN="<YOUR_GHPAT>"' >> ~/.your_shell_profile
        # Example for bash:
        # echo 'export GITHUB_VDK_TOKEN="<YOUR_GHPAT>"' >> ~/.bashrc
        ```
        *Remember to source your profile (`source ~/.your_shell_profile`) or restart your terminal after adding the export.*

    *   **(Temporary) Add `.bin` to PATH:** While under development, you may need to manually add the local binary directory to your PATH:
        ```bash
        echo 'export PATH="$PATH:/<PATH_TO_VDK_REPO>/.bin"' >> ~/.your_shell_profile
        # Example:
        # echo 'export PATH="$PATH:/home/user/projects/VDK/.bin"' >> ~/.bashrc
        ```
        *Source your profile again after adding this.* 

    *   **Flux Secret:** Depending on the setup, you might need to create a Kubernetes secret in the `flux-system` namespace containing your `GITHUB_VDK_TOKEN`. (Details TBD).

4.  **macOS Sequoia/Corporate Proxy Certificate Issue:**
    If you are using macOS Sequoia behind a corporate proxy (like Netskope) and encounter SSL errors (`SSL peer certificate or SSH remote key was not OK (60)`) when Nix downloads packages, you need to add your corporate certificates to Nix's trust store. Follow the steps outlined in [this Nix issue comment](https://github.com/NixOS/nix/issues/8081#issuecomment-1962419263):
    
    *   Generate a combined certificate bundle:
        ```bash
        security export -t certs -f pemseq -k /Library/Keychains/System.keychain -o /tmp/certs-system.pem
        security export -t certs -f pemseq -k /System/Library/Keychains/SystemRootCertificates.keychain -o /tmp/certs-root.pem
        cat /tmp/certs-root.pem /tmp/certs-system.pem > /tmp/ca_cert.pem
        sudo mv /tmp/ca_cert.pem /etc/nix/
        ```
    *   Update `/etc/nix/nix.conf` (create it if it doesn't exist) and add:
        ```ini
        ssl-cert-file = /etc/nix/ca_cert.pem
        ```
    *   Restart the Nix daemon:
        ```bash
        sudo launchctl unload /Library/LaunchDaemons/org.nixos.nix-daemon.plist
        sudo launchctl load /Library/LaunchDaemons/org.nixos.nix-daemon.plist
        ```

## VDK Installation (Binary - Placeholder)

*(Detailed steps will be added here based on the distribution method - e.g., package manager, binary download from releases)*

## Verifying VDK Installation

Once VDK is installed (either via binary or built from source), verify it:

```bash
vega --version
```

Then authenticate once using the device code flow (required before most commands):

```bash
vega login
```

## Next Steps

Now that VDK is set up, learn how to [Create Clusters](../usage/creating-clusters.md).
