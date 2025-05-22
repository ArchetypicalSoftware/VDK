using System;
using Xunit;

public class TestEnvironmentFixture
{
    public TestEnvironmentFixture()
    {
        // Set the environment variable before any tests run
        Environment.SetEnvironmentVariable("VDK_TEST_MODE", "1");
    }
}

