using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MVFC.Veragi.Simulator.TestHelpers;

public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<RecordedLog> _entries = new();

    public IReadOnlyList<RecordedLog> Entries => [.. _entries];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => _entries.Enqueue(new RecordedLog(logLevel, eventId, formatter(state, exception), exception));
}

public sealed record RecordedLog(LogLevel Level, EventId EventId, string Message, Exception? Exception);
