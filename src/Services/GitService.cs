using System.Diagnostics;
using file_content.Abstractions;
using file_content.Enums;

namespace file_content.Services;

public class GitService(IConsoleService consoleService) : IGitService
{
    private static readonly char[] Separator = ['\n', '\r'];

    public async Task<List<string>> GetChangedFilesAsync(GitMode mode, string searchPath)
    {
        if (mode == GitMode.None)
        {
            return [];
        }

        var files = new List<string>();
        try
        {
            var gitRoot = await GetGitRootAsync(searchPath);
            var output = await RunGitCommandAsync("status --porcelain=v1", gitRoot);
            var lines = output.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var status = line.AsSpan(0, 2);
                var path = line[3..];

                var isStaged = !char.IsWhiteSpace(status[0]) && status[0] != '?';
                var isUnstaged = !char.IsWhiteSpace(status[1]);
                var isUntracked = status[0] == '?' && status[1] == '?';

                var include = mode switch
                {
                    GitMode.All => isStaged || isUnstaged || isUntracked,
                    GitMode.Staged => isStaged,
                    GitMode.Changed => isUnstaged || isUntracked,
                    _ => false,
                };

                if (!include)
                {
                    continue;
                }

                string finalPath;
                if (status[0] == 'R' || status[1] == 'R')
                {
                    finalPath = path.Split(" -> ")[1];
                }
                else
                {
                    finalPath = path;
                }

                if (status[0] == 'D' || status[1] == 'D')
                {
                    files.Add($"{Path.Combine(gitRoot, finalPath)} (deleted)");
                }
                else
                {
                    files.Add(Path.Combine(gitRoot, finalPath));
                }
            }
        }
        catch (Exception ex)
        {
            consoleService.WriteLine($"Error getting git files: {ex.Message}");
        }

        return files.Distinct().ToList();
    }

    protected virtual async Task<string> GetGitRootAsync(string path)
    {
        var output = await RunGitCommandAsync("rev-parse --show-toplevel", path);
        return output.Trim();
    }

    protected virtual async Task<string> RunGitCommandAsync(string arguments, string? workingDirectory = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
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
