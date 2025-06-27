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
                var gitFiles = await gitService.GetChangedFilesAsync(gitMode, folderPath);
                foreach (var gitFile in gitFiles)
                {
                    if (gitFile.EndsWith(" (deleted)"))
                    {
                        var originalPath = gitFile.Replace(" (deleted)", "");
                        if (IsFileMatchingExtensions(originalPath, extensionPatterns))
                        {
                            filesToProcess.Add(gitFile);
                        }
                    }
                    else
                    {
                        if (fileService.Exists(gitFile) && IsFileMatchingExtensions(gitFile, extensionPatterns))
                        {
                            filesToProcess.Add(gitFile);
                        }
                    }
                }
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
                if (file.EndsWith(" (deleted)"))
                {
                    stringBuilder.AppendLine($"--- {file} ---");
                }
                else
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
        if (extensionPatterns.Contains("*") || extensionPatterns.Count == 0)
        {
            return true;
        }

        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        extension = extension.TrimStart('.');

        // Check if any pattern matches the file extension
        return extensionPatterns.Any(
            pattern =>
            {
                // Remove the wildcard and dot from the pattern (*.cs -> cs)
                var patternExt = pattern.TrimStart('*', '.');
                return patternExt.Equals(extension, StringComparison.OrdinalIgnoreCase);
            });
    }
}
