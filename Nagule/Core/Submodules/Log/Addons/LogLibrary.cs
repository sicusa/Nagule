namespace Nagule;

using Microsoft.Extensions.Logging;
using Sia;

public class LogLibrary : IAddon
{
    private readonly ILoggerFactory _factory =
        LoggerFactory.Create(builder => builder.AddConsole());

    public void AddProvider(ILoggerProvider provider)
        => _factory.AddProvider(provider);

    public ILogger Create(string categoryName)
        => _factory.CreateLogger(categoryName);

    public ILogger<T> Create<T>()
        => _factory.CreateLogger<T>();
}