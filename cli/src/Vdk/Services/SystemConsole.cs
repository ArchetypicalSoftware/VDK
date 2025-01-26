namespace Vdk.Services;

public class SystemConsole : IConsole
{
    public ConsoleColor ErrorColor { get; set; } = ConsoleColor.DarkRed;
    public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;

    public void Write(string message)
    {
        Console.Write(message);
    }

    public void Write(string template, params object[] parameters)
    {
        Console.Write(template, parameters);
    }

    public void Write(ConsoleColor color, string message)
    {
        var tmp = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Write(message);
        Console.ForegroundColor = tmp;
    }

    public void Write(ConsoleColor color, string template, params object[] parameters)
    {
        var tmp = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Write(template, parameters);
        Console.ForegroundColor = tmp;
    }

    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteLine(string template, params object[] parameters)
    {
        Console.WriteLine(template, parameters);
    }

    public void WriteLine()
    {
        Console.WriteLine();
    }

    public void WriteLine(ConsoleColor color, string message)
    {
        var tmp = Console.ForegroundColor;
        Console.ForegroundColor = color;
        WriteLine(message);
        Console.ForegroundColor = tmp;
    }

    public void WriteLine(ConsoleColor color, string template, params object[] parameters)
    {
        var tmp = Console.ForegroundColor;
        Console.ForegroundColor = color;
        WriteLine(template, parameters);
        Console.ForegroundColor = tmp;
    }

    public void WriteWarning(string message)
    {
        WriteLine(WarningColor, message);
    }

    public void WriteWarning(string template, params object[] parameters)
    {
        WriteLine(WarningColor, template, parameters);
    }

    public void WriteError(string message)
    {
        WriteLine(ErrorColor, message);
    }

    public void WriteError(string template, params object[] parameters)
    {
        WriteLine(ErrorColor, template, parameters);
    }

    public void WriteError(Exception exception)
    {
        var current = exception;
        var prefix = "Exception";
        while (current is not null)
        {
            WriteError(prefix);
            WriteError(current.Message);
            WriteError(exception.StackTrace ?? string.Empty);
            current = current.InnerException;
            prefix = "Inner Exception";
        }
    }

    public void WriteError(Exception exception, string message)
    {
        WriteError(message);
        WriteError(exception);
    }
}