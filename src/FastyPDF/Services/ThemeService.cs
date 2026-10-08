using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using Microsoft.UI.Xaml;

namespace FastyPDF.Services;

public sealed class ThemeService
{
    private readonly ISettingsService _settings;

    public ThemeService(ISettingsService settings)
    {
        _settings = settings;
    }

    public void Apply(FrameworkElement root)
    {
        root.RequestedTheme = _settings.Current.Theme switch
        {
            ThemePreference.Light => ElementTheme.Light,
            ThemePreference.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }
}
