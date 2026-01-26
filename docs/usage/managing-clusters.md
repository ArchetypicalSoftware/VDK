# Managing Vega Clusters

Learn how to manage your Vega-created KinD clusters.

## Listing Clusters

```bash
vega list clusters
```

This lists all Vega-managed clusters. Example output:

```
vdk
my-dev-cluster
production-test
```

## Listing Available Kubernetes Versions

```bash
vega list kubernetes-versions
```

This shows Kubernetes versions available for your installed KinD version.

## Removing Clusters

Remove a specific cluster:

```bash
vega remove cluster --Name my-dev-cluster
# or using the alias
vega remove cluster -n my-dev-cluster
```

To remove the default cluster:

```bash
vega remove cluster
```

## Updating Cluster Certificates

When TLS certificates are renewed, update all clusters:

```bash
vega update clusters
```

This command:

- Scans all Vega clusters for Vega-managed TLS secrets
- Updates certificates that don't match the local certificates
- Restarts gateway deployments to pick up new certificates
- Regenerates nginx reverse proxy configuration

Use verbose mode for debugging:

```bash
vega update clusters --verbose
```

## Infrastructure Management

### Registry

Create the Zot container registry:

```bash
vega create registry
```

Remove it:

```bash
vega remove registry
```

### Reverse Proxy

Create the nginx reverse proxy:

```bash
vega create proxy
```

Remove it:

```bash
vega remove proxy
```

### Cloud Provider KinD

Create the Cloud Provider KinD for load balancer support:

```bash
vega create cloud-provider-kind
```

Remove it:

```bash
vega remove cloud-provider-kind
```

## Using kubectl

Vega automatically configures kubectl contexts for each cluster. After creating a cluster named `my-cluster`, switch to it:

```bash
kubectl config use-context kind-my-cluster
```

Verify connectivity:

```bash
kubectl cluster-info --context kind-my-cluster
```

## Updating KinD Version Information

If you've updated KinD and need to enable new Kubernetes versions:

```bash
vega update kind-version-info
# or using the alias
vega update k8s
```
