namespace file_content.Abstractions;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
