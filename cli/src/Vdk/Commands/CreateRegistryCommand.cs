﻿using System.CommandLine;
using Vdk.Services;
using IConsole = System.CommandLine.IConsole;

namespace Vdk.Commands;

public class CreateRegistryCommand: Command
{
    private readonly IConsole _console;
    private readonly IHubClient _client;

    public CreateRegistryCommand(IConsole console, IHubClient client): base("registry", "Create Vega VDK Container Registry")
    {
        _console = console;
        _client = client;
        this.SetHandler(InvokeAsync);
    }

    public Task InvokeAsync()
    {
        _client.Create();
        return Task.CompletedTask;
    }
}