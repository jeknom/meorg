using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class XUnitLogger<T>(ITestOutputHelper output) : ILogger<T>
{
    private readonly ITestOutputHelper output = output;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        output.WriteLine($"{logLevel,-11} {typeof(T).Name} {formatter(state, exception)}");
        if (exception is not null)
            output.WriteLine(exception.ToString());
    }
}