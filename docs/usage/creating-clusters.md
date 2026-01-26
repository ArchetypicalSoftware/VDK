# Creating KinD Clusters with Vega

This document explains how to create local Kubernetes clusters using Vega.

## Quick Start with Init

The easiest way to get started is using the `init` command, which sets up everything you need:

```bash
vega init
```

This creates:

- The reverse proxy (nginx) for TLS termination
- The container registry (Zot) for local images
- A default KinD cluster

## Basic Cluster Creation

To create a cluster with default settings:

```bash
vega create cluster
```

This creates a cluster with:

- 1 control-plane node
- 2 worker nodes
- Default Kubernetes version (1.29)

## Specifying Cluster Name

```bash
vega create cluster --Name my-dev-cluster
# or using the alias
vega create cluster -n my-dev-cluster
```

## Specifying Kubernetes Version

```bash
vega create cluster --KubeVersion 1.30
# or using the alias
vega create cluster -k 1.30
```

To see available Kubernetes versions:

```bash
vega list kubernetes-versions
```

## Multi-Node Clusters

Create clusters with multiple control-plane and worker nodes:

```bash
# 3 control-plane nodes and 5 workers
vega create cluster --ControlPlaneNodes 3 --Workers 5

# Using aliases
vega create cluster -c 3 -w 5
```

## Combined Example

Create a named cluster with specific configuration:

```bash
vega create cluster --Name production-test --ControlPlaneNodes 1 --Workers 4 --KubeVersion 1.30
```

## What Happens During Cluster Creation

When you create a cluster, Vega:

1. **Ensures infrastructure exists**: Creates the reverse proxy and registry if not present
2. **Validates versions**: Checks that your KinD version supports the requested Kubernetes version
3. **Generates KinD config**: Creates a cluster configuration with proper containerd settings
4. **Creates the cluster**: Invokes KinD to create the actual cluster
5. **Bootstraps Flux**: Sets up GitOps with Flux CD
6. **Configures routing**: Updates the nginx reverse proxy to route traffic to the cluster
