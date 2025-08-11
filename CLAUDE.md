# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# project for creating a tool to manage project documentation. The main technology is Avalonia UI framework for cross-platform desktop applications. The project is designed to be run locally with both console and UI modes, where the UI helps with previewing features and editing.

## Technology Stack

- **.NET 9.0** with C# and nullable reference types enabled
- **Avalonia UI 11.3.3** for cross-platform desktop GUI
- **NUnit** for testing with Avalonia.Headless for UI tests
- Visual Studio solution structure

## Project Structure

The solution follows a standard .NET structure:
- `src/Desktop/` - Main Avalonia desktop application
- `test/Desktop.UITests/` - UI tests using Avalonia.Headless and NUnit
- `doc/` - Design documentation and project structure
- Solution file manages both source and test projects

## Common Commands

### Building
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Desktop/Desktop.csproj
```

### Running
```bash
# Run desktop application
dotnet run --project src/Desktop/Desktop.csproj
```

### Testing
```bash
# Run all tests
dotnet test

# Run UI tests specifically
dotnet test test/Desktop.UITests/Desktop.UITests.csproj
```

### Development
```bash
# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean
```

## Architecture Notes

- Desktop application entry point is `Program.cs` with standard Avalonia bootstrapping
- Uses Avalonia's compiled bindings by default for better performance
- Inter font is configured as the default font
- Diagnostics package is included only in Debug builds
- Project follows design principles: local execution, end-to-end testing, console mode capability with UI for preview/editing

## Development Guidelines

- The project emphasizes learning Avalonia UI framework
- Focus on markdown rendering capabilities
- Maintain both console and UI interaction modes
- Ensure comprehensive end-to-end test coverage for major features