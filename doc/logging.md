# Logging Design Guidelines

This document outlines the logging strategy and guidelines for the Project Documentation Manager application.

## Logging Framework

The application uses **Microsoft.Extensions.Logging** with the following configuration:
- **Console provider** for development and production output
- **Structured logging** with timestamp formatting
- **Category-based filtering** allowing per-namespace log level control

## Log Levels and Usage Guidelines

### Critical (LogCritical)
**Purpose**: Application-threatening errors that require immediate attention.

**When to use**:
- Application startup failures
- Database connection failures (if applicable)
- Security breaches or authentication failures
- Unrecoverable system errors that cause application shutdown

**Example**:
```csharp
logger.LogCritical("Failed to initialize application configuration. Application cannot start.");
```

### Error (LogError)
**Purpose**: Errors that don't crash the application but indicate significant problems.

**When to use**:
- Failed file operations that affect user functionality
- Network request failures
- Unexpected exceptions in business logic
- Failed service initializations that can be recovered

**Example**:
```csharp
logger.LogError(ex, "Failed to save file: {FilePath}", filePath);
logger.LogError("File system monitoring could not be started for: {ProjectFolder}", projectFolder);
```

### Warning (LogWarning)
**Purpose**: Potentially problematic situations that don't prevent operation.

**When to use**:
- Missing or invalid configuration values with fallbacks
- Deprecated feature usage
- Performance issues (slow operations)
- Recoverable errors with degraded functionality

**Example**:
```csharp
logger.LogWarning("DefaultProjectFolder is not configured, using current directory");
logger.LogWarning("File operation took {Duration}ms, which exceeds recommended threshold", duration);
```

### Information (LogInformation)
**Purpose**: General application flow and major state changes.

**When to use**:
- Application lifecycle events (startup, shutdown)
- Major workflow completions (file structure loaded, build completed)
- User-initiated actions with business significance
- Service state changes (monitoring started/stopped)
- Configuration values at startup

**Example**:
```csharp
logger.LogInformation("Application started successfully");
logger.LogInformation("File structure loaded for project: {ProjectPath}", projectPath);
logger.LogInformation("Build completed successfully, {FileCount} files processed", fileCount);
```

**Avoid**:
- Logging every user interaction
- Logging routine operations that happen frequently
- Internal method calls or technical details

### Debug (LogDebug)
**Purpose**: Detailed information for debugging and development troubleshooting.

**When to use**:
- Internal state changes during development
- Method entry/exit points for complex workflows  
- Intermediate values in calculations
- Detailed error context for troubleshooting

**Example**:
```csharp
logger.LogDebug("Processing file structure for directory: {Directory} with {FileCount} files", directory, fileCount);
logger.LogDebug("Cache hit for key: {CacheKey}", key);
```

### Trace (LogTrace)
**Purpose**: Very detailed execution flow, typically for performance analysis.

**When to use**:
- Performance profiling scenarios
- Detailed execution timing
- Very granular state tracking
- Generally not used in production

**Example**:
```csharp
logger.LogTrace("Method {MethodName} executed in {Duration}ms", methodName, duration);
```

## Configuration Strategy

### Default Log Levels
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "LogLevel": {
        "Default": "Information",
        "Desktop": "Information"
      }
    }
  }
}
```

### Environment-Specific Overrides

**Development** (`appsettings.Development.json`):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Desktop": "Debug"
    }
  }
}
```

**Production**:
- Keep Information level for application events
- Warning level for Microsoft framework logs
- Consider structured logging to external systems

## Best Practices

### 1. Use Structured Logging
Always use structured logging with named parameters:

✅ **Good**:
```csharp
logger.LogInformation("User opened file: {FilePath} of size {FileSize} bytes", filePath, fileSize);
```

❌ **Avoid**:
```csharp
logger.LogInformation($"User opened file: {filePath} of size {fileSize} bytes");
```

### 2. Include Context in Error Logs
Provide sufficient context for troubleshooting:

✅ **Good**:
```csharp
logger.LogError(ex, "Failed to process markdown file: {FilePath}, Line: {LineNumber}", filePath, lineNumber);
```

❌ **Avoid**:
```csharp
logger.LogError(ex, "Processing failed");
```

### 3. Performance Considerations
- Avoid expensive operations in log messages
- Use conditional logging for Debug/Trace levels in hot paths
- Consider log message formatting costs

✅ **Good**:
```csharp
if (logger.IsEnabled(LogLevel.Debug))
{
    logger.LogDebug("Complex calculation result: {Result}", GetExpensiveDebugInfo());
}
```

### 4. Security and Privacy
- Never log sensitive information (passwords, tokens, personal data)
- Sanitize file paths if they contain user information
- Be cautious with user input in log messages

❌ **Avoid**:
```csharp
logger.LogInformation("User {Username} logged in with password {Password}", username, password);
```

### 5. Message Consistency
- Use consistent terminology across the application
- Start messages with capital letters
- Use present tense for actions ("Processing file" not "Processed file")
- Include relevant business context

## Category-Based Logging

Use logger categories to enable fine-grained control:

```csharp
// Service-level logging
private readonly ILogger<FileService> logger;

// Configure specific log levels per category
{
  "Logging": {
    "LogLevel": {
      "Desktop.Services.FileService": "Debug",
      "Desktop.ViewModels": "Information"
    }
  }
}
```

## File System Monitoring Specific Guidelines

For the file system monitoring feature:

- **Information**: Service start/stop, major state changes
- **Debug**: Individual file system events during development
- **Warning**: Failed to access files/directories
- **Error**: FileSystemWatcher failures, permission issues

```csharp
// Good examples:
logger.LogInformation("Started file system monitoring for: {ProjectFolder}", projectFolder);
logger.LogDebug("File system change detected: {ChangeType} {Path}", changeType, path);
logger.LogWarning("Access denied to directory: {Path}", path);
logger.LogError(ex, "Failed to start file system monitoring");
```

## Monitoring and Alerting

### Production Monitoring
- Monitor Error and Critical logs for alerting
- Track Warning trends for proactive maintenance
- Use Information logs for business metrics

### Development
- Use Debug level during active development
- Review Warning logs regularly for potential issues
- Ensure Critical/Error logs provide actionable information

## Review Checklist

Before committing logging code, ensure:
- [ ] Appropriate log level for the message importance
- [ ] Structured logging with named parameters
- [ ] No sensitive information in log messages
- [ ] Sufficient context for troubleshooting
- [ ] Consistent terminology and formatting
- [ ] Performance impact considered for high-frequency logs