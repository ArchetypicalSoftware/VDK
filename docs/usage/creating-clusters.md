# Creating KinD Clusters with VDK

This document explains how to create local Kubernetes clusters using VDK.

> Prerequisite: Run `vega login` once to authenticate before creating clusters.

## Basic Cluster Creation

To create a default cluster:

```bash
vega create cluster
```

## Specifying Cluster Name

```bash
vega create cluster --Name my-dev-cluster
```

## Specifying Kubernetes Version

```bash
vega create cluster --KubeVersion 1.29
```

## Multi-Node Clusters

*(Details on creating clusters with multiple control-plane and worker nodes)*

```bash
# Example
vega create cluster --ControlPlaneNodes 1 --Workers 2
```

## Using Configuration Files

*(Details on using a KinD configuration file)*

```bash
# If/when a config file flag is added, document here
``` 
```
