using Spectre.Console.Cli;

namespace ProjectDocumentationManager.Console;

public sealed class TypeRegistrar(IServiceProvider serviceProvider) : ITypeRegistrar
{
    public ITypeResolver Build()
    {
        return new TypeResolver(serviceProvider);
    }

    public void Register(Type service, Type implementation)
    {
        // Not needed since we're using Microsoft.Extensions.DependencyInjection
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // Not needed since we're using Microsoft.Extensions.DependencyInjection
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Not needed since we're using Microsoft.Extensions.DependencyInjection
    }
}

public sealed class TypeResolver(IServiceProvider serviceProvider) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return serviceProvider.GetService(type);
    }

    public void Dispose()
    {
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}