﻿using System.CommandLine;
using Vdk.Services;
using IConsole = Vdk.Services.IConsole;

namespace Vdk.Commands;

public class RemoveRegistryCommand: Command
{
    private readonly IConsole _console;
    private readonly IHubClient _client;

    public RemoveRegistryCommand(IConsole console, IHubClient client) : base("registry", "Remove Vega VDK Container Registry")
    {
        _console = console;
        _client = client;
        this.SetHandler(InvokeAsync);
    }

    public Task InvokeAsync()
    {
        _client.DestroyRegistry();
        return Task.CompletedTask;
    }
}