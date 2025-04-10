# VDK Command Reference

This is a comprehensive reference for all VDK commands.

## `vdk create cluster`

Creates a new KinD cluster.

**Usage:**
`vdk create cluster [flags]`

**Flags:**

*   `--name string`: Name for the cluster (default: `kind`)
*   `--version string`: Kubernetes version to use (e.g., `v1.25.3`). If not specified, uses KinD's default.
*   `--config string`: Path to a KinD configuration file.
*   `--nodes int`: Total number of nodes (control-plane + worker).
*   `--control-planes int`: Number of control-plane nodes.
*   `--wait duration`: Wait time for the control plane to be ready (default: `5m`).

*(Add other commands like `get`, `delete`, `version`, etc., as they are developed)*

## `vdk get clusters`

Lists existing KinD clusters managed by VDK.

## `vdk delete cluster`

Deletes a KinD cluster.

**Flags:**

*   `--name string`: Name of the cluster to delete (default: `kind`)

## `vdk get kubeconfig`

Gets the kubeconfig path for a cluster.

**Flags:**

*   `--name string`: Name of the cluster (default: `kind`)
