# Project Documentation Manager

Personal project for creating a tool for managing project documentation.

Purposes of this project
 * Learn Avalonia
 * Learn to better utilize AI help
 * Investigate markdown rendering.

## Building and Running

### Prerequisites
- .NET 9.0 SDK

### Build
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Desktop/Desktop.csproj
```

### Run
```bash
# Run desktop application
dotnet run --project src/Desktop/Desktop.csproj
```

### Testing
```bash
# Run all tests
dotnet test

# Run UI tests
dotnet test test/Desktop.UITests/Desktop.UITests.csproj

# Run business logic tests
dotnet test test/Business.Tests/Business.Tests.csproj
```
