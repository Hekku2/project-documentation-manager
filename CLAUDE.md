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

The solution follows a standard .NET structure with clear separation of concerns:
- `src/Desktop/` - Main Avalonia desktop application with MVVM architecture
- `src/Business/` - Core business logic for markdown processing
- `test/Desktop.UITests/` - UI tests using Avalonia.Headless and NUnit  
- `test/Business.Tests/` - Unit tests for business logic
- `doc/` - Design documentation and project structure
- `example-projects/` - Sample projects demonstrating markdown template features

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

# Run business logic tests
dotnet test test/Business.Tests/Business.Tests.csproj

# Run specific test by name
dotnet test --filter "TestMethodName"

# Run tests with specific pattern
dotnet test --filter "MainWindow_Should_Allow_Folder_Expansion"

# Run log-related tests
dotnet test --filter "FullyQualifiedName~Log"
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
- Hybrid hosting model: .NET Host + Avalonia UI integration with shared service provider

### Business Logic Architecture
The `Business` project contains the core markdown processing functionality:
- **Services Layer**: `IMarkdownFileCollectorService`, `IMarkdownCombinationService`, `IMarkdownDocumentFileWriterService`
- **Models**: `MarkdownDocument` for representing markdown files
- **Template System**: Processes `.mdext` template files with `.mdsrc` source inclusions
- **File Processing**: Collects, combines, and writes processed markdown documentation

### MVVM Pattern
- **ViewModels**: All inherit from `ViewModelBase` which implements `INotifyPropertyChanged`
- **Views**: AXAML files with code-behind, use compiled bindings for performance
- **Models**: Simple data classes like `FileSystemItem`, `EditorTab`
- **Services**: Business logic layer (e.g., `IFileService`, `FileService`)
- **Commands**: `RelayCommand` implementation for UI actions and event handling

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
- Current settings: `DefaultProjectFolder`, `DefaultOutputFolder`

### Dynamic Logging System
The application implements a sophisticated logging architecture:
- **IDynamicLoggerProvider**: Allows adding logger providers after host creation
- **UILoggerProvider**: Displays logs in the application's UI log output panel
- **Thread-Safe UI Updates**: Uses `Dispatcher.UIThread.Post()` for UI marshaling
- **Runtime Reconfiguration**: Logging providers can be added after host startup
- **Dual Output**: Logs appear in both console (development) and UI (user-facing)

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

### Editor Tab System
- **Multi-Tab Interface**: Similar to VS Code with tab bar and content area
- **Active Tab Management**: Only one tab active at a time with visual highlighting
- **File Loading**: Async file content loading with tab creation
- **Tab Operations**: Close, select, and switch between tabs seamlessly

### UI Layout Architecture
- **Split Layout**: File explorer (left) + editor area (right) with splitter
- **Dual Panel Editor**: File editor (top) + log output (bottom) with tabs
- **Responsive Design**: DockPanel and Grid layouts for proper space utilization
- **Theme Integration**: Dark theme with consistent color scheme throughout

## Development Guidelines

- The project emphasizes learning Avalonia UI framework
- Focus on markdown rendering capabilities
- Maintain both console and UI interaction modes
- Ensure comprehensive end-to-end test coverage for major features
- Use NSubstitute for mocking in tests rather than custom mock classes
- Follow MVVM pattern with proper separation of concerns

### Data Model Design Guidelines
- **Prefer `required` keyword**: For data transfer objects and models, use `required` for non-nullable properties that don't have meaningful default values
- **Avoid meaningless defaults**: Don't use `string.Empty` or similar defaults for properties that should always have real values
- **Type safety first**: The `required` keyword provides compile-time enforcement and better developer experience
- **Self-documenting code**: `required` clearly indicates essential vs optional properties
- **Example pattern**:
  ```csharp
  public class ExampleDto
  {
      public required string Id { get; set; }           // Essential, no meaningful default
      public required string Name { get; set; }         // Essential, no meaningful default
      public string Description { get; set; } = "";     // Optional, empty is meaningful
      public bool IsActive { get; set; } = false;       // Optional, false is meaningful
      public DateTime? LastModified { get; set; }       // Optional, null is meaningful
  }
  ```

### Test-Driven Development Requirements
- **CRITICAL**: After ANY code change, feature addition, or modification, ALWAYS run `dotnet test` to ensure all tests pass
- If tests fail after code changes, fix the failing tests immediately before considering the task complete
- Never leave broken tests - all tests must pass before marking a task as done
- When adding new features, consider if new tests are needed and update existing tests that may be affected
- Test failures indicate either bugs in the code or outdated test expectations that need updating

### Code Validation Process
- **Build Validation**: Use `dotnet build` to verify code compiles without errors
- **Test Validation**: Use `dotnet test` to ensure functionality works as expected
- **DO NOT**: Run the desktop application (`dotnet run --project src/Desktop/Desktop.csproj`) for validation purposes
- The desktop application is for manual testing and demonstration, not automated validation
- **CRITICAL BUILD REQUIREMENT**: ALWAYS run `dotnet build` on the ENTIRE solution after ANY code changes to ensure nothing is broken
- **Full Solution Validation**: Never assume that individual project builds or test runs guarantee the whole solution builds correctly
- **Immediate Fix Requirement**: If `dotnet build` fails, all build errors must be fixed immediately before proceeding with any other tasks

## important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.