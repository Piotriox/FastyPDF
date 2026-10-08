namespace FastyPDF.Core.Abstractions;

public interface IAppLog
{
    void Info(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
