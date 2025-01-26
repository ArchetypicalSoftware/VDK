namespace Vdk.Services;

public interface IShell
{
    void Execute(string command, string[] args);
    string ExecuteAndCapture(string command, string[] args);
}