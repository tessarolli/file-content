#!/bin/bash

# Exit on error
set -e

# Build the tool
echo "Building file-contents..."
dotnet publish -r osx-x64 -c Release

# Create installation directory
INSTALL_DIR="/usr/local/bin"
echo "Installing to $INSTALL_DIR..."

# Copy the binary
sudo cp "bin/Release/net9.0/osx-x64/publish/file-contents" "$INSTALL_DIR/"

# Make it executable
sudo chmod +x "$INSTALL_DIR/file-contents"

echo "Installation complete! You can now use 'file-contents' from anywhere."
echo "Try it with: file-contents --help"
