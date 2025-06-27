using file_content.Abstractions;
using file_content.Enums;
using Moq;

namespace file_content.UnitTests;

public class AppRunnerTests
{
    private readonly AppRunner _appRunner;
    private readonly Mock<IClipboardService> _clipboardServiceMock;
    private readonly Mock<IConsoleService> _consoleServiceMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<IGitService> _gitServiceMock;

    public AppRunnerTests()
    {
        _fileServiceMock = new Mock<IFileService>();
        _gitServiceMock = new Mock<IGitService>();
        _clipboardServiceMock = new Mock<IClipboardService>();
        _consoleServiceMock = new Mock<IConsoleService>();

        _appRunner = new AppRunner(
            _fileServiceMock.Object,
            _gitServiceMock.Object,
            _clipboardServiceMock.Object,
            _consoleServiceMock.Object);
    }

    [Fact]
    public async Task RunAsync_WithCsExtension_ProcessesOnlyCsFiles()
    {
        // Arrange
        var files = new List<string>
        {
            "test1.cs",
        };
        _fileServiceMock.Setup(f => f.EnumerateFiles(".", "*.cs", SearchOption.TopDirectoryOnly)).Returns(files);
        _fileServiceMock.Setup(f => f.ReadAllTextAsync("test1.cs")).ReturnsAsync("cs content");

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            ["cs"],
            GitMode.None);

        // Assert
        _consoleServiceMock.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("cs content"))), Times.Once);
        _fileServiceMock.Verify(f => f.ReadAllTextAsync("test2.js"), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WithGitChangedMode_ProcessesChangedFiles()
    {
        // Arrange
        const string fullPathToChangedFile = "/test/repo/changed.cs";
        var gitFiles = new List<string>
        {
            fullPathToChangedFile,
        };
        _gitServiceMock.Setup(g => g.GetChangedFilesAsync(GitMode.Changed, It.IsAny<string>())).ReturnsAsync(gitFiles);
        SetupFileMock(fullPathToChangedFile, "changed content");

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            ["cs"],
            GitMode.Changed);

        // Assert
        _consoleServiceMock.Verify(
            c => c.WriteLine(
                It.Is<string>(
                    s =>
                        s.Contains("--- Contents of /test/repo/changed.cs ---") &&
                        s.Contains("changed content")
                )),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ToClipboard_CopiesToClipboard()
    {
        // Arrange
        var files = new List<string>
        {
            "file.txt",
        };
        _fileServiceMock.Setup(f => f.EnumerateFiles(".", "*.txt", SearchOption.TopDirectoryOnly)).Returns(files);
        _fileServiceMock.Setup(f => f.ReadAllTextAsync("file.txt")).ReturnsAsync("file content");

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Clipboard,
            ["txt"],
            GitMode.None);

        // Assert
        _clipboardServiceMock.Verify(c => c.SetTextAsync(It.Is<string>(s => s.Contains("file content"))), Times.Once);
        _consoleServiceMock.Verify(c => c.WriteLine("Contents of 1 files copied to clipboard."), Times.Once);
    }

    [Fact]
    public async Task RunAsync_NoFilesFound_DisplaysInfoMessage()
    {
        // Arrange
        _fileServiceMock
            .Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(new List<string>());

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            ["cs"],
            GitMode.None);

        // Assert
        _consoleServiceMock.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("No files found"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_Recursive_SearchesAllDirectories()
    {
        // Arrange
        var files = new List<string>
        {
            "test1.cs",
        };
        _fileServiceMock.Setup(f => f.EnumerateFiles(".", "*.cs", SearchOption.AllDirectories)).Returns(files);
        _fileServiceMock.Setup(f => f.ReadAllTextAsync("test1.cs")).ReturnsAsync("recursive content");

        // Act
        await _appRunner.RunAsync(
            ".",
            true,
            OutputMode.Console,
            ["cs"],
            GitMode.None);

        // Assert
        _fileServiceMock.Verify(f => f.EnumerateFiles(".", "*.cs", SearchOption.AllDirectories), Times.Once);
        _consoleServiceMock.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("recursive content"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_FileReadError_DisplaysErrorMessage()
    {
        // Arrange
        var files = new List<string>
        {
            "error.txt",
        };
        _fileServiceMock
            .Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Returns(files);
        _fileServiceMock.Setup(f => f.ReadAllTextAsync("error.txt")).ThrowsAsync(new Exception("Read error"));

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            new List<string>
            {
                "txt",
            },
            GitMode.None);

        // Assert
        _consoleServiceMock.Verify(
            c => c.WriteLine(It.Is<string>(s => s.Contains("Error reading file: Read error"))),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_DirectoryNotFound_DisplaysErrorMessage()
    {
        // Arrange
        _fileServiceMock
            .Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
            .Throws(new DirectoryNotFoundException("Directory not found"));

        // Act
        await _appRunner.RunAsync(
            "nonexistent-dir",
            false,
            OutputMode.Console,
            new List<string>(),
            GitMode.None);

        // Assert
        _consoleServiceMock.Verify(c => c.WriteLine("An error occurred: Directory not found"), Times.Once);
    }

    [Fact]
    public async Task RunAsync_NoExtensionsProvided_ProcessesAllFiles()
    {
        // Arrange
        var files = new List<string>
        {
            "file1.txt",
            "file2.log",
        };
        _fileServiceMock.Setup(f => f.EnumerateFiles(".", "*", SearchOption.TopDirectoryOnly)).Returns(files);
        _fileServiceMock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync("some content");

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            new List<string>(),
            GitMode.None);

        // Assert
        _fileServiceMock.Verify(f => f.ReadAllTextAsync("file1.txt"), Times.Once);
        _fileServiceMock.Verify(f => f.ReadAllTextAsync("file2.log"), Times.Once);
    }

    [Fact]
    public async Task RunAsync_GitModeWithNoChanges_ProcessesNoFiles()
    {
        // Arrange
        _gitServiceMock
            .Setup(g => g.GetChangedFilesAsync(GitMode.Staged, It.IsAny<string>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            new List<string>
            {
                "cs",
            },
            GitMode.Staged);

        // Assert
        _fileServiceMock.Verify(f => f.ReadAllTextAsync(It.IsAny<string>()), Times.Never);
        _consoleServiceMock.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("No files found"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_GitModeWithUnmatchedExtensions_ProcessesNoFiles()
    {
        // Arrange
        var gitFiles = new List<string>
        {
            "/path/to/file1.txt",
        };
        _gitServiceMock.Setup(g => g.GetChangedFilesAsync(GitMode.Staged, It.IsAny<string>())).ReturnsAsync(gitFiles);
        _fileServiceMock.Setup(f => f.Exists("/path/to/file1.txt")).Returns(true);

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            new List<string>
            {
                "cs",
            },
            GitMode.Staged);

        // Assert
        _fileServiceMock.Verify(f => f.ReadAllTextAsync(It.IsAny<string>()), Times.Never);
        _consoleServiceMock.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("No files found"))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_GitModeWithAllFileTypes_ProcessesCorrectly()
    {
        // Arrange
        var gitFiles = new List<string>
        {
            "/mock/path/added.cs",
            "/mock/path/modified.cs",
            "/mock/path/deleted.cs (deleted)",
            "/mock/path/renamed.cs",
            "/mock/path/untracked.txt",
        };
        _gitServiceMock.Setup(g => g.GetChangedFilesAsync(GitMode.All, It.IsAny<string>())).ReturnsAsync(gitFiles);

        SetupFileMock("/mock/path/added.cs", "added content");
        SetupFileMock("/mock/path/modified.cs", "modified content");
        SetupFileMock("/mock/path/renamed.cs", "renamed content");
        SetupFileMock("/mock/path/untracked.txt", "untracked content");

        // Act
        await _appRunner.RunAsync(
            ".",
            false,
            OutputMode.Console,
            new List<string>
            {
                "cs",
                "txt",
            },
            GitMode.All);

        // Assert
        _consoleServiceMock.Verify(
            c => c.WriteLine(
                It.Is<string>(
                    s =>
                        s.Contains("--- Contents of /mock/path/added.cs ---") &&
                        s.Contains("added content") &&
                        s.Contains("--- Contents of /mock/path/modified.cs ---") &&
                        s.Contains("modified content") &&
                        s.Contains("--- /mock/path/deleted.cs (deleted) ---") &&
                        s.Contains("--- Contents of /mock/path/renamed.cs ---") &&
                        s.Contains("renamed content") &&
                        s.Contains("--- Contents of /mock/path/untracked.txt ---") &&
                        s.Contains("untracked content")
                )),
            Times.Once);

        _fileServiceMock.Verify(f => f.ReadAllTextAsync(It.Is<string>(p => p.Contains("deleted.cs"))), Times.Never);
    }

    private void SetupFileMock(string fullPath, string content)
    {
        _fileServiceMock.Setup(f => f.Exists(fullPath)).Returns(true);
        _fileServiceMock.Setup(f => f.ReadAllTextAsync(fullPath)).ReturnsAsync(content);
    }
}
