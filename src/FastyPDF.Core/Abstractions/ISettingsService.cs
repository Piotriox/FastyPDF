using FastyPDF.Core.Models;

namespace FastyPDF.Core.Abstractions;

public interface ISettingsService
{
    AppSettings Current { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
