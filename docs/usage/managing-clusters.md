# Managing VDK Clusters

Learn how to manage your VDK-created KinD clusters.

## Listing Clusters

```bash
vega list clusters
```

Expected Output:
```
NAME              STATUS    VERSION
kind              Running   v1.25.3
my-dev-cluster    Running   v1.25.3
```

## Deleting Clusters

```bash
vega remove cluster --Name my-dev-cluster
```

To delete the default 'kind' cluster:
```bash
vega remove cluster --Name vega
```

## Getting Kubeconfig

VDK typically configures `kubectl` automatically. To get the path to the kubeconfig file for a specific cluster:

```bash
vega get kubeconfig --Name my-dev-cluster
```

Or for the default cluster:
```bash
vega get kubeconfig --Name vega
```
