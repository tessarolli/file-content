using System.Text;
using file_content.Abstractions;
using file_content.Enums;

namespace file_content;

public class AppRunner(
    IFileService fileService,
    IGitService gitService,
    IClipboardService clipboardService,
    IConsoleService consoleService)
{
    public async Task RunAsync(
        string folderPath,
        bool recursive,
        OutputMode output,
        List<string> extensions,
        GitMode gitMode)
    {
        var stringBuilder = new StringBuilder();
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        try
        {
            var extensionPatterns = extensions.Select(ext => $"*.{ext.TrimStart('.')}").ToList();
            if (extensionPatterns.Count == 0)
            {
                extensionPatterns.Add("*");
            }

            var filesToProcess = new List<string>();
            if (gitMode != GitMode.None)
            {
                var gitFiles = await gitService.GetChangedFilesAsync(gitMode);
                filesToProcess.AddRange(
                    gitFiles
                        .Select(gitFile => fileService.GetFullPath(Path.Combine(folderPath, gitFile)))
                        .Where(
                            fullPath => fileService.Exists(fullPath) &&
                                        IsFileMatchingExtensions(fullPath, extensionPatterns)));
            }
            else
            {
                filesToProcess.AddRange(
                    extensionPatterns.SelectMany(
                        pattern => fileService.EnumerateFiles(folderPath, pattern, searchOption)));
            }

            var processedFiles = 0;
            foreach (var file in filesToProcess.Distinct())
            {
                stringBuilder.AppendLine($"--- Contents of {file} ---");
                try
                {
                    stringBuilder.AppendLine(await fileService.ReadAllTextAsync(file));
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
                consoleService.WriteLine($"No files found matching the specified criteria in '{folderPath}'.");
                return;
            }

            if (output == OutputMode.Clipboard)
            {
                await clipboardService.SetTextAsync(result);
                consoleService.WriteLine($"Contents of {processedFiles} files copied to clipboard.");
            }
            else
            {
                consoleService.WriteLine(result);
            }
        }
        catch (Exception ex)
        {
            consoleService.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private bool IsFileMatchingExtensions(string filePath, List<string> extensionPatterns)
    {
        foreach (var pattern in extensionPatterns)
        {
            if (pattern == "*")
                return true;

            var extension = fileService.GetFileExtension(filePath);
            if (string.IsNullOrEmpty(extension))
                return false;

            extension = extension.TrimStart('.');
            var patternExtension = pattern.TrimStart('*', '.');
            if (extension.Equals(patternExtension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
