using file_content.Abstractions;
using file_content.Services;

namespace file_content.UnitTests;

/// <summary>
///     Helper class for testing GitService by allowing us to mock the git command output
/// </summary>
public class MockableGitService : GitService
{
    public MockableGitService(IConsoleService consoleService) : base(consoleService)
    {
    }

    public string MockGitStatusOutput { get; set; } = string.Empty;
    public string MockGitRootOutput { get; set; } = "/mock/git/root";
    public bool ShouldThrowOnRunGitCommand { get; set; }

    protected override Task<string> GetGitRootAsync(string path)
    {
        return Task.FromResult(MockGitRootOutput);
    }

    protected override Task<string> RunGitCommandAsync(string arguments, string? workingDirectory = null)
    {
        if (ShouldThrowOnRunGitCommand)
        {
            throw new Exception("Mock git command error");
        }

        if (arguments.Contains("status"))
        {
            return Task.FromResult(MockGitStatusOutput);
        }

        return Task.FromResult(MockGitRootOutput);
    }
}
