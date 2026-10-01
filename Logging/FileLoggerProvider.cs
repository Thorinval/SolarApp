using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SolarApp.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _logFilePath;
    private readonly object _syncRoot = new();

    public FileLoggerProvider(string logsDirectoryPath)
    {
        Directory.CreateDirectory(logsDirectoryPath);
        _logFilePath = Path.Combine(logsDirectoryPath, $"solarapp-{DateTime.Now:yyyyMMdd}.log");
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logFilePath, _syncRoot));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private readonly object _syncRoot;

        public FileLogger(string categoryName, string logFilePath, object syncRoot)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
            _syncRoot = syncRoot;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            var lines = new List<string>
            {
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{logLevel}] {_categoryName}: {message}"
            };

            if (exception is not null)
            {
                lines.Add(exception.ToString());
            }

            lock (_syncRoot)
            {
                File.AppendAllLines(_logFilePath, lines);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
