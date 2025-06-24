#Requires -RunAsAdministrator

# Exit on error
$ErrorActionPreference = 'Stop'

# Build the tool
Write-Host "Building file-contents for Windows..."
dotnet publish -r win-x64 -c Release

# Set installation directory
$installDir = "$env:ProgramFiles\file-contents"

# Create installation directory if it doesn't exist
if (-not (Test-Path -Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir
}

# Copy the binary and other published files
Write-Host "Installing to $installDir..."
Copy-Item -Path "src\bin\Release\net9.0\win-x64\publish\*" -Destination $installDir -Recurse -Force

# Add the installation directory to the system's PATH
$currentPath = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine')
if (-not ($currentPath -split ';' -contains $installDir)) {
    $newPath = "$currentPath;$installDir"
    [System.Environment]::SetEnvironmentVariable('PATH', $newPath, 'Machine')
    Write-Host "Added $installDir to the system PATH."
} else {
    Write-Host "Installation directory is already in the system PATH."
}

Write-Host "Installation complete! You can now use 'file-contents' from anywhere."
Write-Host "Note: You may need to restart your terminal for the PATH changes to take effect."
Write-Host "Try it with: file-contents --help"
