using FastyPDF.Core.Abstractions;

namespace FastyPDF.Core.Services;

public sealed class FileAppLog : IAppLog
{
    private readonly object _sync = new();
    private readonly string _filePath;

    public FileAppLog()
    {
        AppPaths.EnsureCreated();
        _filePath = Path.Combine(AppPaths.Logs, $"fastypdf-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public void Info(string message) => Write("INFO", message, null);

    public void Warning(string message) => Write("WARN", message, null);

    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        try
        {
            var line = $"{DateTime.UtcNow:O} [{level}] {message}";
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_sync)
            {
                File.AppendAllText(_filePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw into the UI flow.
        }
    }
}
