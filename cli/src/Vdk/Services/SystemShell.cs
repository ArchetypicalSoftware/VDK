using System.Diagnostics;

namespace Vdk.Services;

public class SystemShell : IShell
{
    private readonly IConsole _console;

    public SystemShell(IConsole console)
    {
        _console = console;
    }

    public void Execute(string command, string[] args)
    {
        
        
        var cmd = new ProcessStartInfo(command)
        {
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        var process = new Process
        {
            StartInfo = cmd
        };
        process.OutputDataReceived += Process_OutputDataReceived;

        process.EnableRaisingEvents = true;
        
        process.ErrorDataReceived += Process_OutputDataReceived;

        process.Start();

        process.BeginOutputReadLine();

        process.WaitForExit();
    }

    public string ExecuteAndCapture(string command, string[] args)
    {
        var cmd = new ProcessStartInfo(command)
        {
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        using var process = new Process { StartInfo = cmd };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _console.WriteLine(e.Data);
        }
    }
}