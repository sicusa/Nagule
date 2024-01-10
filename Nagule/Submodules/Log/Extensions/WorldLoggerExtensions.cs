namespace Nagule;

using Microsoft.Extensions.Logging;
using Sia;

public static class WorldLoggerExtensions
{
    public static ILogger CreateLogger(this World world, string categoryName)
        => world.AcquireAddon<LogLibrary>().Create(categoryName);

    public static ILogger<T> CreateLogger<T>(this World world)
        => world.AcquireAddon<LogLibrary>().Create<T>();
}