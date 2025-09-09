# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# console application for managing project documentation through markdown template processing. The tool processes `.mdext` template files and `.mdsrc` source files to generate combined markdown documentation. It's designed as a command-line tool that can be packaged and distributed as a .NET global tool.

## Technology Stack

- **.NET 9.0** with C# and nullable reference types enabled
- **Spectre.Console 0.47.0** for rich command-line interface and styling
- **Microsoft.Extensions.Hosting 9.0.0** for dependency injection and configuration
- **NUnit 4.3.1** for testing framework
- **NSubstitute 5.3.0** for mocking in tests
- Console application with global tool packaging support

## Project Structure

The solution follows a standard .NET structure with clear separation of concerns:
- `src/Console/` - Main console application with CLI commands using Spectre.Console
- `src/Business/` - Core business logic for markdown processing and file operations
- `test/Business.Tests/` - Unit tests for business logic services
- `test/Console.Tests/` - Unit tests for console commands
- `test/Console.AcceptanceTests/` - End-to-end acceptance tests with test data
- `doc/` - Design documentation and project structure
- `example-projects/` - Sample projects demonstrating markdown template features

## Common Commands

### Building
```bash
# Build entire solution
dotnet build

# Build console project specifically
dotnet build src/Console/Console.csproj

# Build business logic project
dotnet build src/Business/Business.csproj
```

### Running
```bash
# Run console application with combine command
dotnet run --project src/Console/Console.csproj -- combine --input ./example-projects/basic-features --output ./output

# Run console application with validate command  
dotnet run --project src/Console/Console.csproj -- validate --input ./example-projects/basic-features

# Install as global tool (after building)
dotnet pack src/Console/Console.csproj
dotnet tool install --global --add-source ./src/Console/nupkg ProjectDocumentationManager.Console

# Run as global tool
project-docs combine --input ./example-projects/basic-features --output ./output
project-docs validate --input ./example-projects/basic-features
```

### Testing
```bash
# Run all tests
dotnet test

# Run business logic tests specifically
dotnet test test/Business.Tests/Business.Tests.csproj

# Run console command tests
dotnet test test/Console.Tests/Console.Tests.csproj

# Run acceptance tests
dotnet test test/Console.AcceptanceTests/Console.AcceptanceTests.csproj

# Run specific test by name
dotnet test --filter "TestMethodName"

# Run tests with pattern matching
dotnet test --filter "FullyQualifiedName~MarkdownCombination"
```

### Development
```bash
# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean
```

## Architecture Overview

### Console Application Architecture
The console application uses **Spectre.Console.Cli** for command-line interface:
- `Program.cs` sets up the host builder with DI services and configures Spectre.Console CommandApp
- Commands inherit from Spectre.Console.Cli base classes with proper argument/option handling
- `TypeRegistrar` bridges Microsoft.Extensions.DependencyInjection with Spectre.Console DI
- Commands: `CombineCommand` (processes templates) and `ValidateCommand` (validates templates)

### Business Logic Architecture
The `Business` project contains the core markdown processing functionality:
- **Services Layer**: `IMarkdownFileCollectorService`, `IMarkdownCombinationService`, `IMarkdownDocumentFileWriterService`
- **Models**: `MarkdownDocument` for representing markdown files with filename, path, and content
- **Template System**: Processes `.mdext` template files with `.mdsrc` source inclusions
- **File Processing**: Collects, combines, and writes processed markdown documentation
- **Validation**: Validates template syntax and source file references

### Dependency Injection & Configuration
The application uses Microsoft.Extensions.Hosting for DI container:
- Services registered in `Program.CreateDefaultBuilder()` configuration
- Business services registered as transient dependencies
- Commands registered as transient for Spectre.Console.Cli integration
- Logging configured with console provider for development feedback

### File Processing Pipeline
1. **Collection**: `IMarkdownFileCollectorService` recursively finds `.mdext` and `.mdsrc` files
2. **Combination**: `IMarkdownCombinationService` processes template syntax and includes source content  
3. **Writing**: `IMarkdownDocumentFileWriterService` outputs processed markdown files
4. **Validation**: Template syntax validation and source file reference checking

## Testing Strategy

### Unit Testing Patterns
- Use `NSubstitute.For<IInterface>()` for creating mocks of service dependencies
- Use `Assert.Multiple()` for grouping related assertions in test methods
- Mock data structures use modern C# collection expressions and required properties
- Tests verify both service behavior and data transformation logic

### Acceptance Testing
- End-to-end tests using real file system operations with test data
- `ConsoleTestBase` provides common test infrastructure for command execution
- Test data organized in `TestData/` with realistic scenarios (BasicScenario, ErrorScenario)
- Tests verify complete workflows from input files to output generation

### Logger Testing Guidelines
- **Use NullLoggerFactory**: Use `NullLoggerFactory.Instance.CreateLogger<T>()` for all test scenarios
- **Don't test logging calls**: Logger calls should not be verified or tested - focus on business logic instead
- **Treat loggers as infrastructure**: Loggers are infrastructure concerns, not business logic to be tested
- **Example pattern**:
  ```csharp
  // Preferred: NullLogger (no overhead, no verification needed)
  var logger = NullLoggerFactory.Instance.CreateLogger<MyService>();
  var service = new MyService(logger);
  
  // Avoid: Mocking or verifying logger calls
  var logger = Substitute.For<ILogger<MyService>>();
  logger.Received().LogInformation("message"); // Don't do this
  ```

## Key Implementation Details

### Template Processing System
- `.mdext` files are template files containing markdown with special include syntax
- `.mdsrc` files are source files containing reusable markdown content
- Template syntax allows including source files with path resolution
- Recursive directory scanning for file collection with async/await patterns
- Error handling for missing source files and circular dependencies

### Command-Line Interface
- **Spectre.Console.Cli** provides rich command parsing and validation
- Commands use strongly-typed settings classes with attributes for arguments/options
- Built-in help generation and error handling with styled console output
- Support for input/output directory specification and validation

### Global Tool Packaging
- `PackAsTool=true` and `ToolCommandName=project-docs` enable global tool installation
- Package metadata configured for NuGet distribution
- Tool can be installed globally and used from any directory
- Version management through standard .NET packaging

## Development Guidelines

### Data Model Design Guidelines
- **Prefer `required` keyword**: For data transfer objects and models, use `required` for non-nullable properties that don't have meaningful default values
- **Avoid meaningless defaults**: Don't use `string.Empty` or similar defaults for properties that should always have real values
- **Type safety first**: The `required` keyword provides compile-time enforcement and better developer experience
- **Self-documenting code**: `required` clearly indicates essential vs optional properties
- **Example pattern**:
  ```csharp
  public class MarkdownDocument
  {
      public required string FileName { get; init; }     // Essential, no meaningful default
      public required string FilePath { get; init; }     // Essential, no meaningful default  
      public required string Content { get; init; }      // Essential, no meaningful default
  }
  ```

### Primary Constructor Guidelines
- **Prefer primary constructors**: Use primary constructors when feasible for cleaner, more concise code
- **Dependency injection scenarios**: Primary constructors work well with DI containers and reduce boilerplate
- **Use parameters directly**: Reference constructor parameters directly instead of creating private fields when possible
- **Avoid when complex initialization needed**: Use traditional constructors when complex setup logic is required

### Test-Driven Development Requirements
- **CRITICAL**: After ANY code change, feature addition, or modification, ALWAYS run `dotnet test` to ensure all tests pass
- If tests fail after code changes, fix the failing tests immediately before considering the task complete
- Never leave broken tests - all tests must pass before marking a task as done
- When adding new features, consider if new tests are needed and update existing tests that may be affected
- Test failures indicate either bugs in the code or outdated test expectations that need updating

### Code Validation Process
- **Build Validation**: Use `dotnet build` to verify code compiles without errors
- **Test Validation**: Use `dotnet test` to ensure functionality works as expected
- **CRITICAL BUILD REQUIREMENT**: ALWAYS run `dotnet build` on the ENTIRE solution after ANY code changes to ensure nothing is broken
- **Full Solution Validation**: Never assume that individual project builds or test runs guarantee the whole solution builds correctly
- **Immediate Fix Requirement**: If `dotnet build` fails, all build errors must be fixed immediately before proceeding with any other tasks

## Template File Format

### Template Files (.mdext)
Template files use standard markdown with special include syntax for embedding source files.

### Source Files (.mdsrc) 
Source files contain reusable markdown content that can be included in templates.

### Processing Rules
- Recursive directory scanning for template and source file discovery
- Path resolution for source file includes relative to template file location
- Validation of template syntax and source file references
- Output generation maintains directory structure from input

## important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.