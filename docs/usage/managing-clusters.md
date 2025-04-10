# Managing VDK Clusters

Learn how to manage your VDK-created KinD clusters.

## Listing Clusters

```bash
vdk get clusters
```

Expected Output:
```
NAME              STATUS    VERSION
kind              Running   v1.25.3
my-dev-cluster    Running   v1.25.3
```

## Deleting Clusters

```bash
vdk delete cluster --name my-dev-cluster
```

To delete the default 'kind' cluster:
```bash
vdk delete cluster
```

## Getting Kubeconfig

VDK typically configures `kubectl` automatically. To get the path to the kubeconfig file for a specific cluster:

```bash
vdk get kubeconfig --name my-dev-cluster
```

Or for the default cluster:
```bash
vdk get kubeconfig
```
