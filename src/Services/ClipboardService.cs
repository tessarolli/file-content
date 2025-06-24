using file_content.Abstractions;

namespace file_content.Services;

public class ClipboardService(IConsoleService consoleService) : IClipboardService
{
    public async Task SetTextAsync(string text)
    {
        try
        {
            await TextCopy.ClipboardService.SetTextAsync(text);
        }
        catch (Exception ex)
        {
            consoleService.WriteLine($"Could not copy to clipboard: {ex.Message}. Displaying in console instead.");
            consoleService.WriteLine(text);
        }
    }
}
