using System.Text;
using System.Diagnostics;
using TextCopy;

namespace file_content
{
    /// <summary>
    /// Specifies the output destination for the file contents.
    /// </summary>
    public enum OutputMode
    {
        /// <summary>
        /// Output to the console.
        /// </summary>
        Console,

        /// <summary>
        /// Output to the clipboard.
        /// </summary>
        Clipboard,
    }

    /// <summary>
    /// Specifies the Git mode for file selection.
    /// </summary>
    public enum GitMode
    {
        /// <summary>
        /// No Git integration.
        /// </summary>
        None,

        /// <summary>
        /// Select changed (modified and untracked) files.
        /// </summary>
        Changed,

        /// <summary>
        /// Select staged (added) files.
        /// </summary>
        Staged,

        /// <summary>
        /// Select all changed and staged files.
        /// </summary>
        All,
    }

    /// <summary>
    /// Main class for the file-content CLI tool.
    /// </summary>
    public abstract class Program
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
            List<string> extensions = ["cs"];
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

            // Process files based on the parsed options
            await DisplayFileContents(folderPath, recursive, output, extensions, gitMode);
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

        /// <summary>
        /// Reads the contents of the specified files and outputs them to the console or clipboard.
        /// </summary>
        /// <param name="folderPath">The path to the folder to read from.</param>
        /// <param name="recursive">Whether to search recursively.</param>
        /// <param name="output">The output destination.</param>
        /// <param name="extensions">The list of file extensions to include.</param>
        /// <param name="gitMode">The Git mode for file selection.</param>
        private static async Task DisplayFileContents(
            string folderPath,
            bool recursive,
            OutputMode output,
            List<string> extensions,
            GitMode gitMode)
        {
            var folder = new DirectoryInfo(folderPath);
            var stringBuilder = new StringBuilder();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                // Convert extensions to a list of glob patterns
                var extensionPatterns = extensions.Select(ext => $"*.{ext.TrimStart('.')}").ToList();

                // If no extensions are provided, include all files
                if (extensionPatterns.Count == 0)
                {
                    extensionPatterns.Add("*");
                }

                var processedFiles = 0;

                // Get the list of files to process based on Git mode or directory scan
                List<FileInfo> filesToProcess = [];

                if (gitMode != GitMode.None)
                {
                    var gitFiles = await GetGitChangedFiles(gitMode);
                    filesToProcess.AddRange(
                        gitFiles
                            .Select(gitFile => Path.GetFullPath(gitFile, folderPath))
                            .Select(fullPath => new FileInfo(fullPath))
                            .Where(
                                fileInfo => fileInfo.Exists && IsFileMatchingExtensions(fileInfo, extensionPatterns)));
                }
                else
                {
                    // Get files by iterating through specified patterns
                    filesToProcess.AddRange(
                        extensionPatterns.SelectMany(pattern => folder.EnumerateFiles(pattern, searchOption)));
                }

                // Read and append the content of each file
                foreach (var file in filesToProcess)
                {
                    stringBuilder.AppendLine($"--- Contents of {file.FullName} ---");
                    try
                    {
                        stringBuilder.AppendLine(await File.ReadAllTextAsync(file.FullName));
                    }
                    catch (Exception ex)
                    {
                        stringBuilder.AppendLine($"Error reading file: {ex.Message}");
                    }

                    stringBuilder.AppendLine();
                    processedFiles++;
                }

                var result = stringBuilder.ToString();

                if (processedFiles == 0)
                {
                    Console.WriteLine($"No files found matching the specified criteria in {folder.FullName}.");
                    return;
                }

                // Output the result to the specified destination
                if (output == OutputMode.Clipboard)
                {
                    await SetClipboardTextAsync(result);
                    Console.WriteLine($"Contents of {processedFiles} files copied to clipboard.");
                }
                else
                {
                    Console.WriteLine(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the specified text to the system clipboard.
        /// </summary>
        /// <param name="text">The text to copy to the clipboard.</param>
        private static async Task SetClipboardTextAsync(string text)
        {
            try
            {
                // Using TextCopy package for cross-platform clipboard support
                await ClipboardService.SetTextAsync(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not copy to clipboard: {ex.Message}. Displaying in console instead.");
                Console.WriteLine(text);
            }
        }

        /// <summary>
        /// Checks if a file matches any of the specified extension patterns.
        /// </summary>
        /// <param name="file">The file to check.</param>
        /// <param name="extensionPatterns">The list of glob patterns for extensions.</param>
        /// <returns>True if the file matches, otherwise false.</returns>
        private static bool IsFileMatchingExtensions(FileInfo file, List<string> extensionPatterns)
        {
            foreach (var pattern in extensionPatterns)
            {
                // A wildcard pattern matches any file
                if (pattern == "*")
                {
                    return true;
                }

                var extension = file.Extension.TrimStart('.');

                // Extract the extension part from the pattern (e.g., "*.cs" -> "cs")
                var patternExtension = pattern.TrimStart('*', '.');

                if (extension.Equals(patternExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// A separator for splitting command output into lines.
        /// </summary>
        private static readonly char[] separator = ['\n', '\r'];

        /// <summary>
        /// Gets a list of files from Git based on the specified mode.
        /// </summary>
        /// <param name="mode">The Git mode (Changed, Staged, or All).</param>
        /// <returns>A list of file paths.</returns>
        private static async Task<List<string>> GetGitChangedFiles(GitMode mode)
        {
            var files = new List<string>();

            try
            {
                var gitArgs = "";

                switch (mode)
                {
                    case GitMode.Changed:
                        // Get modified and untracked files
                        gitArgs = "ls-files --modified --others --exclude-standard";
                        break;
                    case GitMode.Staged:
                        // Get staged files
                        gitArgs = "diff --name-only --cached";
                        break;
                    case GitMode.All:
                        // Get both changed and staged files
                        gitArgs = "ls-files --modified --others --exclude-standard";
                        var stagedOutput = await RunGitCommand("diff --name-only --cached");
                        foreach (var line in stagedOutput.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                        {
                            files.Add(line.Trim());
                        }

                        break;
                }

                // Run the git command and add the files to the list
                var output = await RunGitCommand(gitArgs);
                foreach (var line in output.Split(separator, StringSplitOptions.RemoveEmptyEntries))
                {
                    files.Add(line.Trim());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting git files: {ex.Message}");
            }

            return files;
        }

        /// <summary>
        /// Runs a Git command and returns the standard output.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the Git command.</param>
        /// <returns>The standard output of the command.</returns>
        private static async Task<string> RunGitCommand(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return output;
            }

            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Git command failed with exit code {process.ExitCode}: {error}");
        }
    }
}
