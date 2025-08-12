# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# project for creating a tool to manage project documentation. The main technology is Avalonia UI framework for cross-platform desktop applications. The project is designed to be run locally with both console and UI modes, where the UI helps with previewing features and editing.

## Technology Stack

- **.NET 9.0** with C# and nullable reference types enabled
- **Avalonia UI 11.3.3** for cross-platform desktop GUI
- **NUnit** for testing with Avalonia.Headless for UI tests
- **NSubstitute 5.3.0** for mocking in tests
- **Microsoft.Extensions.Hosting** for dependency injection and configuration
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

# Run specific test by name
dotnet test --filter "TestMethodName"

# Run tests with specific pattern
dotnet test --filter "MainWindow_Should_Allow_Folder_Expansion"
```

### Development
```bash
# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean
```

## Architecture Overview

### Dependency Injection & Configuration
The application uses Microsoft.Extensions.Hosting for DI container and configuration management:
- `Program.cs` sets up the host builder with services and configuration
- `appsettings.json` contains application configuration including `ApplicationOptions`
- Services are registered in `Program.CreateHostBuilder()` and accessed via DI
- `App.ServiceProvider` provides access to services from the Avalonia application

### MVVM Pattern
- **ViewModels**: All inherit from `ViewModelBase` which implements `INotifyPropertyChanged`
- **Views**: AXAML files with code-behind, use compiled bindings for performance
- **Models**: Simple data classes like `FileSystemItem`
- **Services**: Business logic layer (e.g., `IFileService`, `FileService`)

### File System Management
The application features a file explorer with lazy loading:
- **FileSystemItemViewModel**: Implements lazy loading of child directories
- **Background Loading**: Children loaded asynchronously when folders are expanded
- **TreeView Layout**: Uses DockPanel to fill available vertical space
- **Loading States**: Visual indicators for loading operations

### Configuration System
- `ApplicationOptions` class defines available settings
- `appsettings.json` provides default configuration
- Configuration bound to strongly-typed options via `IOptions<T>`
- Current settings: `DefaultProjectFolder`, `DefaultTheme`

## Testing Strategy

### UI Testing with Avalonia.Headless
- Tests use `[AvaloniaTest]` attribute for UI components
- **NSubstitute** for mocking services like `IFileService`
- **Async Testing**: Proper delays and waiting for lazy loading operations
- **Comprehensive Coverage**: Tests for TreeView expansion, lazy loading, and UI layout

### Test Patterns
- Use `NSubstitute.For<IInterface>()` for creating mocks
- Use `Assert.Multiple()` for grouping related assertions
- Mock data structures use modern C# collection expressions
- Tests verify both UI state and underlying data model state

## Key Implementation Details

### Lazy Loading Implementation
- `FileSystemItemViewModel` loads children only when `IsExpanded` is set to true
- Uses `Task.Run()` for background loading with `Dispatcher.UIThread.Post()` for UI updates
- Placeholder "Loading..." items shown until real children are loaded
- `_childrenLoaded` flag prevents duplicate loading operations

### TreeView Expansion Logic
- Root folder auto-expands via `isRoot` parameter in constructor
- Child folders remain collapsed by default
- Expansion triggers lazy loading of children
- Loading state indicators (⏳) shown during background operations

## Development Guidelines

- The project emphasizes learning Avalonia UI framework
- Focus on markdown rendering capabilities
- Maintain both console and UI interaction modes
- Ensure comprehensive end-to-end test coverage for major features
- Use NSubstitute for mocking in tests rather than custom mock classes
- Follow MVVM pattern with proper separation of concerns