# Creating KinD Clusters with VDK

This document explains how to create local Kubernetes clusters using VDK.

## Basic Cluster Creation

To create a default cluster:

```bash
vdk create cluster
```

## Specifying Cluster Name

```bash
vdk create cluster --name my-dev-cluster
```

## Specifying Kubernetes Version

```bash
vdk create cluster --version v1.25.3
```

## Multi-Node Clusters

*(Details on creating clusters with multiple control-plane and worker nodes)*

```bash
# Example (syntax TBD)
vdk create cluster --nodes 3 --control-planes 1
```

## Using Configuration Files

*(Details on using a KinD configuration file)*

```bash
vdk create cluster --config path/to/kind-config.yaml
```
