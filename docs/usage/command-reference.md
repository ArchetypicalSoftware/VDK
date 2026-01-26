# Vega Command Reference

This is a comprehensive reference for all Vega CLI commands.

## Root Commands

### `vega init`

Initializes the complete Vega development environment. This command:
- Creates the reverse proxy (nginx)
- Creates the container registry (Zot)
- Creates a default KinD cluster

**Usage:**
```bash
vega init
```

---

## Create Commands

### `vega create cluster`

Creates a new KinD cluster with Vega configuration.

**Usage:**
```bash
vega create cluster [flags]
```

**Flags:**

| Flag | Alias | Default | Description |
|------|-------|---------|-------------|
| `--Name` | `-n` | `vdk` | Name for the cluster |
| `--ControlPlaneNodes` | `-c` | `1` | Number of control-plane nodes |
| `--Workers` | `-w` | `2` | Number of worker nodes |
| `--KubeVersion` | `-k` | `1.29` | Kubernetes API version |

**Example:**
```bash
vega create cluster --Name my-cluster --Workers 3 --KubeVersion 1.30
```

### `vega create registry`

Creates the Vega container registry (Zot) for local image storage.

**Usage:**
```bash
vega create registry
```

### `vega create proxy`

Creates the Vega reverse proxy (nginx) for TLS termination.

**Usage:**
```bash
vega create proxy
```

### `vega create cloud-provider-kind`

Creates a Cloud Provider KIND container that provisions load balancers for services in KinD clusters.

**Usage:**
```bash
vega create cloud-provider-kind
```

---

## Remove Commands

### `vega remove cluster`

Removes a KinD cluster.

**Usage:**
```bash
vega remove cluster [flags]
```

**Flags:**

| Flag | Alias | Default | Description |
|------|-------|---------|-------------|
| `--Name` | `-n` | `vdk` | Name of the cluster to remove |

**Example:**
```bash
vega remove cluster --Name my-cluster
```

### `vega remove registry`

Removes the Vega container registry.

**Usage:**
```bash
vega remove registry
```

### `vega remove proxy`

Removes the Vega reverse proxy.

**Usage:**
```bash
vega remove proxy
```

### `vega remove cloud-provider-kind`

Removes the Cloud Provider KIND container.

**Usage:**
```bash
vega remove cloud-provider-kind
```

---

## List Commands

### `vega list clusters`

Lists all VDK-managed KinD clusters.

**Usage:**
```bash
vega list clusters
```

### `vega list kubernetes-versions`

Lists available Kubernetes versions for the installed KinD version.

**Usage:**
```bash
vega list kubernetes-versions
```

---

## Update Commands

### `vega update kind-version-info`

Updates the KinD version information cache. This maps KinD versions to available Kubernetes versions and enables support for new Kubernetes releases.

**Usage:**
```bash
vega update kind-version-info
# or use the alias:
vega update k8s
```

### `vega update clusters`

Updates cluster configurations including TLS certificates. This command:
- Checks all VDK clusters for outdated certificates
- Updates Vega-managed TLS secrets (`dev-tls` or annotated with `vega.dev/managed=true`)
- Restarts gateway deployments to pick up new certificates
- Regenerates nginx reverse proxy configuration

**Usage:**
```bash
vega update clusters [flags]
```

**Flags:**

| Flag | Alias | Description |
|------|-------|-------------|
| `--verbose` | `-v` | Enable verbose output for debugging |

**Example:**
```bash
vega update clusters --verbose
```


