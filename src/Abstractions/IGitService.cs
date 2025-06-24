using file_content.Enums;

namespace file_content.Abstractions;

public interface IGitService
{
    Task<List<string>> GetChangedFilesAsync(GitMode mode);
}
