using System;
using Xunit;

// This class will be constructed by xUnit before any tests run
public class TestSetup : IDisposable
{
    static TestSetup()
    {
        // Ensure the test environment variable is set for all tests
        Environment.SetEnvironmentVariable("VDK_TEST_MODE", "1");
    }

    public TestSetup() { }
    public void Dispose() { }
}

