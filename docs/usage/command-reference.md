# Vega Command Reference

This is a comprehensive reference for all Vega commands.

## `vega login`

Authenticate using the OAuth2 Device Code flow. Supports multiple profiles.

**Usage:**
`vega login [--profile <name>]`

## `vega logout`

Remove local credentials for the current or specified profile.

**Usage:**
`vega logout [--profile <name>]`

## `vega create cluster`

Creates a new KinD cluster.

**Usage:**
`vega create cluster [flags]`

**Flags:**

*   `--Name, -n string`: Name for the cluster (default: `vega` with auto-increment if taken)
*   `--KubeVersion, -k string`: Kubernetes version (CLI resolves compatible image for KinD version)
*   `--ControlPlaneNodes, -c int`: Number of control-plane nodes (default configured in CLI)
*   `--Workers, -w int`: Number of worker nodes (default configured in CLI)

*(Add other commands like `get`, `delete`, `version`, etc., as they are developed)*

## `vega list clusters`

Lists existing KinD clusters managed by Vega.

## `vega remove cluster`

Deletes a KinD cluster.

**Flags:**

*   `--Name, -n string`: Name of the cluster to delete

## `vega list kubernetes-versions`

Lists supported Kubernetes versions for your installed KinD version.

## `vega get kubeconfig`

Gets the kubeconfig path for a cluster.

**Flags:**

*   `--Name, -n string`: Name of the cluster (default: `vega`)

# `vega create cloud-provider-kind`

Creates a cloud Provider KIND docker image which runs as a standalone binary in the local machine 
and will connect to the Kind cluster and provision new Load balancer containers for the services.
