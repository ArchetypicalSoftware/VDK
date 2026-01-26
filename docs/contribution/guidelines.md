# Contribution Guidelines

We welcome contributions to the Vega Development Kit (VDK)!

## How to Contribute

1. **Fork the Repository:** Start by forking the main VDK repository.
2. **Clone your Fork:** Clone your forked repository to your local machine.
3. **Create a Branch:** Create a new branch for your feature or bug fix (`git checkout -b feature/your-feature-name` or `bugfix/issue-number`).
4. **Make Changes:** Implement your changes and add relevant tests.
5. **Run Tests:** Ensure all tests pass with `dotnet test`.
6. **Commit Changes:** Commit your changes with clear and descriptive messages.
7. **Push to Your Fork:** Push your changes to your forked repository.
8. **Open a Pull Request:** Open a pull request against the main VDK repository's `main` branch.

## Code Style

- Follow standard C# conventions and .NET naming guidelines
- Use nullable reference types (`#nullable enable`)
- Prefer async/await for I/O operations
- Keep methods focused and single-purpose
- Add XML documentation for public APIs

## Testing

- Add unit tests for new functionality
- Ensure existing tests pass before submitting
- Run tests with: `dotnet test`

## Reporting Bugs

Please [open an issue](https://github.com/ArchetypicalSoftware/VDK/issues) on the GitHub repository, providing:

- Steps to reproduce the bug
- Expected behavior
- Actual behavior
- Environment details (OS, .NET version, Docker version)
