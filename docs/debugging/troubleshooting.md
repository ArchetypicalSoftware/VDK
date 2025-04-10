# Troubleshooting VDK Issues

Common issues and how to resolve them.

## Cluster Creation Fails

*   **Check Docker:** Ensure Docker Desktop or Docker Engine is running.
*   **Check Resources:** KinD requires sufficient RAM and CPU. Check Docker's resource limits.
*   **Examine KinD Logs:** VDK uses KinD under the hood. You can often find more detailed logs using KinD's export functionality:
    ```bash
    kind export logs --name <cluster-name> ./kind-logs
    ```
    Replace `<cluster-name>` with the name of the cluster that failed (or `kind` if default).
*   **Network Issues:** Firewalls or VPNs can sometimes interfere with KinD's networking.

## Cannot Connect to Cluster (`kubectl` errors)

*   **Verify Kubeconfig:** Ensure `kubectl` is using the correct context. Run `kubectl config current-context`. It should point to `kind-<cluster-name>`.
*   **Check Cluster Status:** Use `vdk get clusters` to see if the cluster is running.
*   **Check `kubectl` Proxy Settings:** Ensure no conflicting proxy settings are interfering.

## VDK Command Not Found

*   Ensure the VDK binary's location is in your system's `PATH` environment variable.
*   Verify the installation completed successfully.

## Reporting Issues

If you can't resolve the issue, please [open an issue](link-to-issues-page) on GitHub, providing:

*   VDK version (`vdk --version`)
*   Operating System
*   Docker version
*   Command run
*   Full output/error message
*   Steps to reproduce
