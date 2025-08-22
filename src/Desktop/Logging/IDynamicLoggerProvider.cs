using Microsoft.Extensions.Logging;

namespace Desktop.Logging;

public interface IDynamicLoggerProvider : ILoggerProvider
{
    void AddLoggerProvider(ILoggerProvider provider);
    void RemoveLoggerProvider(ILoggerProvider provider);
    void ClearProviders();
}