namespace Vdk.Services;

public interface IConsole
{
    ConsoleColor ErrorColor { get; set; }
    ConsoleColor WarningColor { get; set; }
    void WriteLine();
    void Write(string message);
    void Write(string template, params object[] parameters);
    void Write(ConsoleColor color, string message);
    void Write(ConsoleColor color, string template, params object[] parameters);
    void WriteLine(string message);
    void WriteLine(string template, params object[] parameters);
    void WriteLine(ConsoleColor color, string message);
    void WriteLine(ConsoleColor color, string template, params object[] parameters);
    void WriteWarning(string message);
    void WriteWarning(string template, params object[] parameters);
    void WriteError(string message);
    void WriteError(string template, params object[] parameters);
    void WriteError(Exception exception);
    void WriteError(Exception exception, string message);
}