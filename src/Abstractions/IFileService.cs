namespace file_content.Abstractions;

public interface IFileService
{
    bool Exists(string path);
    Task<string> ReadAllTextAsync(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    string GetFullPath(string path);
    string GetFileExtension(string path);
}
