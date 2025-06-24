# file-contents

A fast, AOT-compiled .NET CLI tool for displaying the contents of files in a folder with support for filtering by extension and output to clipboard or console.

## Features

- 🚀 **Blazing Fast**: Compiled with Native AOT for instant startup
- 📋 **Clipboard Support**: Copy file contents to clipboard with a single command
- 🔍 **Flexible Filtering**: Filter files by extension (supports multiple extensions)
- 🔄 **Recursive Search**: Search through subdirectories with a simple flag
- 📱 **Cross-Platform**: Works on Windows, macOS, and Linux
- 🐙 **Git Integration**: Process only changed, staged, or all files in a Git repository

## Installation

Ensure you have the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later installed, then follow the instructions for your operating system.

### Windows

1. Open PowerShell as an Administrator.
2. Navigate to the project directory.
3. Run the installation script:
   ```powershell
   .\install-windows.ps1
   ```

### macOS

1. Open your terminal.
2. Navigate to the project directory.
3. Run the installation script:
   ```bash
   ./install-macos.sh
   ```

### Linux

1. Open your terminal.
2. Navigate to the project directory.
3. Run the installation script:
   ```bash
   ./install-linux.sh
   ```

## Usage

```bash
# Show help
file-contents --help

# Basic usage (shows .cs files in current directory, copies to clipboard)
file-contents

# Show files in a specific directory
file-contents --folder /path/to/directory

# Search recursively through subdirectories
file-contents --recursive

# Output to console instead of clipboard
file-contents --output Console

# Show only specific file extensions
file-contents --extensions cs js ts

# Combine options
file-contents --folder src --recursive --extensions cs --output Console

# Process only changed files and output to console
file-contents --git Changed -o Console
```

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--folder` | `-f` | The folder to read files from | Current directory |
| `--recursive` | `-r` | Search for files recursively | `false` |
| `--output` | `-o` | Output destination: `Console` or `Clipboard` | `Clipboard` |
| `--extensions` | `-e` | File extensions to include (without leading dot) | `cs` |
| `--git` | `-g` | Git mode: `None`, `Changed`, `Staged`, or `All` | `None` |
| `--help` | `-h` | Show help information | |

## Examples

1. **Basic usage**
   ```bash
   # Shows all .cs files in current directory, copies to clipboard
   file-contents
   ```

2. **Search in a specific directory**
   ```bash
   file-contents --folder ~/projects/myapp
   ```

3. **Search for multiple file types**
   ```bash
   file-contents --extensions cs js ts
   ```

4. **View output in console**
   ```bash
   file-contents --output Console
   ```

5. **Recursive search**
   ```bash
   file-contents --recursive
   ```



## License

MIT
