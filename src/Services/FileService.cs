using file_content.Abstractions;

namespace file_content.Services;

public class FileService : IFileService
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public Task<string> ReadAllTextAsync(string path)
    {
        return File.ReadAllTextAsync(path);
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return new DirectoryInfo(path).EnumerateFiles(searchPattern, searchOption).Select(f => f.FullName);
    }

    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public string GetFileExtension(string path)
    {
        return Path.GetExtension(path);
    }
}
