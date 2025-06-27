using file_content.Abstractions;
using file_content.Enums;
using file_content.Services;
using Moq;

namespace file_content.UnitTests;

public class GitServiceTests
{
    private readonly Mock<IConsoleService> _consoleServiceMock;
    private readonly GitService _gitService;

    public GitServiceTests()
    {
        _consoleServiceMock = new Mock<IConsoleService>();
        _gitService = new GitService(_consoleServiceMock.Object);
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithGitModeNone_ReturnsEmptyList()
    {
        // Act
        var result = await _gitService.GetChangedFilesAsync(GitMode.None, ".");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithGitError_HandlesExceptionGracefully()
    {
        // Arrange - Use reflection to replace the private method with one that throws
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.ShouldThrowOnRunGitCommand = true;

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.All, ".");

        // Assert
        Assert.Empty(result);
        _consoleServiceMock.Verify(
            c => c.WriteLine(It.Is<string>(s => s.Contains("Error getting git files"))),
            Times.Once);
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithStagedMode_FiltersCorrectly()
    {
        // Arrange
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.MockGitStatusOutput = @"M  staged.cs
 M unstaged.cs
?? untracked.cs
";

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.Staged, ".");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => Path.GetFileName(f) == "staged.cs");
        Assert.DoesNotContain(result, f => Path.GetFileName(f) == "unstaged.cs");
        Assert.DoesNotContain(result, f => Path.GetFileName(f) == "untracked.cs");
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithChangedMode_FiltersCorrectly()
    {
        // Arrange
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.MockGitStatusOutput = @"M  staged.cs
 M unstaged.cs
?? untracked.cs
";

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.Changed, ".");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, f => Path.GetFileName(f) == "staged.cs");
        Assert.Contains(result, f => Path.GetFileName(f) == "unstaged.cs");
        Assert.Contains(result, f => Path.GetFileName(f) == "untracked.cs");
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithAllMode_IncludesAllFiles()
    {
        // Arrange
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.MockGitStatusOutput = @"M  staged.cs
 M unstaged.cs
?? untracked.cs
";

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.All, ".");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, f => Path.GetFileName(f) == "staged.cs");
        Assert.Contains(result, f => Path.GetFileName(f) == "unstaged.cs");
        Assert.Contains(result, f => Path.GetFileName(f) == "untracked.cs");
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithDeletedFiles_MarksThemAsDeleted()
    {
        // Arrange
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.MockGitStatusOutput = @"D  staged-deleted.cs
 D unstaged-deleted.cs
";

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.All, ".");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.EndsWith("staged-deleted.cs (deleted)"));
        Assert.Contains(result, f => f.EndsWith("unstaged-deleted.cs (deleted)"));
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithRenamedFiles_HandlesRenamedFiles()
    {
        // Arrange
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.MockGitStatusOutput = @"R  old-name.cs -> new-name.cs
";

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.All, ".");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => Path.GetFileName(f) == "new-name.cs");
        Assert.DoesNotContain(result, f => Path.GetFileName(f) == "old-name.cs");
    }

    [Fact]
    public async Task GetChangedFilesAsync_WithDuplicateFiles_ReturnDistinctFiles()
    {
        // Arrange
        var mockGitService = new MockableGitService(_consoleServiceMock.Object);
        mockGitService.MockGitStatusOutput = @"MM duplicate.cs
";

        // Act
        var result = await mockGitService.GetChangedFilesAsync(GitMode.All, ".");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, f => Path.GetFileName(f) == "duplicate.cs");
    }
}
