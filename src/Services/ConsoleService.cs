using file_content.Abstractions;

namespace file_content.Services;

public class ConsoleService : IConsoleService
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }
}
