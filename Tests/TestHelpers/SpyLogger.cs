using Microsoft.Extensions.Logging;

namespace Tests.TestHelpers;

public class SpyLogger<T> : ILogger<T>
{
    private readonly List<(LogLevel Level, string Message, Exception? Ex)> _entries = new();

    public bool HasErrorOrAbove => _entries.Any(e => e.Level >= LogLevel.Error);
    public bool HasWarningOrAbove => _entries.Any(e => e.Level >= LogLevel.Warning);
    public IReadOnlyList<(LogLevel Level, string Message, Exception? Ex)> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
        => _entries.Add((logLevel, formatter(state, exception), exception));
}
