using Moq;
using Vdk.Services;

namespace Vdk.Tests;

public abstract class CommandTestBase
{
    protected Mock<IConsole> MockConsole { get; } = new Mock<IConsole>();
    // Add other common mocks or helpers here if needed
}
