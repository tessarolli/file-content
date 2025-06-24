using file_content.Enums;
using file_content.Services;

namespace file_content;

/// <summary>
/// Main class for the file-content CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static async Task Main(string[] args)
    {
        // Default values for command-line options
        var folderPath = ".";
        var recursive = false;
        var output = OutputMode.Clipboard;
        var extensions = new List<string> { "cs" };
        var gitMode = GitMode.None;

        // Parse command line arguments
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLower();
            switch (arg)
            {
                case "--folder":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        folderPath = args[++i];
                    }

                    break;

                case "--recursive":
                case "-r":
                    recursive = true;
                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        if (Enum.TryParse<OutputMode>(args[++i], true, out var outputMode))
                        {
                            output = outputMode;
                        }
                    }

                    break;

                case "--extensions":
                case "-e":
                    extensions.Clear();

                    // Consume all subsequent arguments that are not options
                    while (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                    {
                        extensions.Add(args[++i]);
                    }

                    break;

                case "--git":
                case "-g":
                    if (i + 1 < args.Length)
                    {
                        if (Enum.TryParse<GitMode>(args[++i], true, out var mode))
                        {
                            gitMode = mode;
                        }
                    }

                    break;

                case "--help":
                case "-h":
                    DisplayHelp();
                    return;
            }
        }

        // Set up dependency injection
        var consoleService = new ConsoleService();
        var clipboardService = new ClipboardService(consoleService);
        var gitService = new GitService(consoleService);
        var fileService = new FileService();
        var appRunner = new AppRunner(fileService, gitService, clipboardService, consoleService);

        // Process files based on the parsed options
        await appRunner.RunAsync(folderPath, recursive, output, extensions, gitMode);
    }

    /// <summary>
    /// Displays the help message with usage instructions and options.
    /// </summary>
    private static void DisplayHelp()
    {
        Console.WriteLine("file-contents - Displays the contents of all files in a folder");
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  file-contents [options]");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  --folder, -f <path>       The folder to read files from (default: current directory)");
        Console.WriteLine("  --recursive, -r           Search for files recursively (default: false)");
        Console.WriteLine(
            "  --output, -o <mode>       Output destination: Console or Clipboard (default: Clipboard)");
        Console.WriteLine(
            "  --extensions, -e <exts>   File extensions to include without leading dot (default: cs)");
        Console.WriteLine("  --git, -g <mode>          Git mode: None, Changed, Staged, or All (default: None)");
        Console.WriteLine("  --help, -h                Display this help message");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  file-contents");
        Console.WriteLine("  file-contents --folder /path/to/project --recursive");
        Console.WriteLine("  file-contents -e js ts -o Console");
        Console.WriteLine("  file-contents --git Changed -o Console");
    }
}
