using System.Diagnostics;
using file_content.Abstractions;
using file_content.Enums;

namespace file_content.Services;

public class GitService(IConsoleService consoleService) : IGitService
{
    private static readonly char[] Separator = ['\n', '\r'];

    public async Task<List<string>> GetChangedFilesAsync(GitMode mode)
    {
        var files = new List<string>();
        try
        {
            var gitArgs = "";
            switch (mode)
            {
                case GitMode.Changed:
                    gitArgs = "ls-files --modified --others --exclude-standard";
                    break;
                case GitMode.Staged:
                    gitArgs = "diff --name-only --cached";
                    break;
                case GitMode.All:
                    gitArgs = "ls-files --modified --others --exclude-standard";
                    var stagedOutput = await RunGitCommandAsync("diff --name-only --cached");
                    files.AddRange(
                        stagedOutput
                            .Split(Separator, StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => line.Trim()));

                    break;
                case GitMode.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            if (!string.IsNullOrEmpty(gitArgs))
            {
                var output = await RunGitCommandAsync(gitArgs);
                files.AddRange(
                    output.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()));
            }
        }
        catch (Exception ex)
        {
            consoleService.WriteLine($"Error getting git files: {ex.Message}");
        }

        return files;
    }

    private static async Task<string> RunGitCommandAsync(string arguments)
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
            return output;

        var error = await process.StandardError.ReadToEndAsync();
        throw new Exception($"Git command failed with exit code {process.ExitCode}: {error}");
    }
}
