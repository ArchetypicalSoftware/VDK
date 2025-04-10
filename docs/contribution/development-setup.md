# Development Setup

Setting up your environment to contribute to VDK.

## Prerequisites

-   [Go](https://go.dev/doc/install) (Specify required version, e.g., 1.19+)
-   [Docker](https://docs.docker.com/get-docker/)
-   [Make](https://www.gnu.org/software/make/) (Optional, if using Makefiles)
-   [Git](https://git-scm.com/)

## Building from Source

1.  Clone the repository (or your fork).
2.  Navigate to the project directory.
3.  Run the build command:

    ```bash
    go build -o vdk ./cmd/vdk
    # Or, if using Make:
    # make build
    ```

## Running Tests

```bash
go test ./...
# Or, if using Make:
# make test
```

## Linting

*(Instructions for running linters, e.g., `golangci-lint`)*
