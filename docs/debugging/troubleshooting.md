# Troubleshooting Vega Issues

Common issues and how to resolve them.

## Cluster Creation Fails

- **Check Docker:** Ensure Docker Desktop or Docker Engine is running.
- **Check Resources:** KinD requires sufficient RAM and CPU. Check Docker's resource limits.
- **Examine KinD Logs:** Vega uses KinD under the hood. You can find detailed logs using:

  ```bash
  kind export logs --name <cluster-name> ./kind-logs
  ```

  Replace `<cluster-name>` with the name of the cluster that failed (or `vdk` for the default).
- **Network Issues:** Firewalls or VPNs can sometimes interfere with KinD's networking.
- **KinD Version Mismatch:** If you get version-related errors, update the version info:

  ```bash
  vega update kind-version-info
  ```

## Cannot Connect to Cluster (kubectl errors)

- **Verify Kubeconfig:** Ensure `kubectl` is using the correct context:

  ```bash
  kubectl config current-context
  ```

  It should show `kind-<cluster-name>`.
- **Check Cluster Status:** Use `vega list clusters` to see if the cluster exists.
- **Check Container Status:** Verify the cluster containers are running:

  ```bash
  docker ps --filter name=<cluster-name>
  ```

- **Check kubectl Proxy Settings:** Ensure no conflicting proxy settings are interfering.

## Vega Command Not Found

- Ensure the Vega binary's location is in your system's `PATH` environment variable.
- If using Devbox, make sure you're in a `devbox shell` session.
- Verify the installation completed successfully.

## Registry Issues

If the container registry isn't working:

- **Check if running:**

  ```bash
  docker ps --filter name=vega-registry
  ```

- **Check registry logs:**

  ```bash
  docker logs vega-registry
  ```

- **Recreate the registry:**

  ```bash
  vega remove registry
  vega create registry
  ```

## Reverse Proxy Issues

If the nginx reverse proxy isn't routing correctly:

- **Check if running:**

  ```bash
  docker ps --filter name=vega-proxy
  ```

- **Check proxy logs:**

  ```bash
  docker logs vega-proxy
  ```

- **Port conflicts:** If port 443 is in use, set a different port:

  ```bash
  export REVERSE_PROXY_HOST_PORT=8443
  ```

- **Regenerate configuration:**

  ```bash
  vega update clusters
  ```

## Certificate Issues

If TLS certificates are expired or mismatched:

- **Update certificates in all clusters:**

  ```bash
  vega update clusters --verbose
  ```

- **Check certificate files exist:**
  - `Certs/fullchain.pem`
  - `Certs/privkey.pem`

## Reporting Issues

If you can't resolve the issue, please [open an issue](https://github.com/ArchetypicalSoftware/VDK/issues) on GitHub, providing:

- Vega version (`vega --version`)
- Operating System
- Docker version (`docker --version`)
- KinD version (`kind --version`)
- Command run
- Full output/error message
- Steps to reproduce
