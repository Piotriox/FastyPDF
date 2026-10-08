using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Models;
using FastyPDF.Services;
using Microsoft.UI.Xaml;

namespace FastyPDF.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IRecentFilesService _recent;
    private readonly ThemeService _theme;
    private readonly PickerService _pickers;

    public SettingsViewModel(
        ISettingsService settings,
        IRecentFilesService recent,
        ThemeService theme,
        PickerService pickers)
    {
        _settings = settings;
        _recent = recent;
        _theme = theme;
        _pickers = pickers;
        Themes =
        [
            new ThemeOption(ThemePreference.System, "Sistem varsayılanı"),
            new ThemeOption(ThemePreference.Light, "Açık"),
            new ThemeOption(ThemePreference.Dark, "Koyu")
        ];
        SelectedTheme = Themes.First(item => item.Value == settings.Current.Theme);
        DefaultOutputFolder = settings.Current.DefaultOutputFolder ?? string.Empty;
    }

    public IReadOnlyList<ThemeOption> Themes { get; }

    [ObservableProperty]
    private ThemeOption _selectedTheme;

    [ObservableProperty]
    private string _defaultOutputFolder = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        _ = ApplyThemeAsync(value);
    }

    [RelayCommand]
    private async Task PickOutputFolderAsync()
    {
        var folder = await _pickers.PickFolderAsync();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        DefaultOutputFolder = folder;
        _settings.Current.DefaultOutputFolder = folder;
        await _settings.SaveAsync();
        StatusMessage = "Varsayılan klasör kaydedildi.";
    }

    [RelayCommand]
    private async Task ClearRecentAsync()
    {
        await _recent.ClearAsync();
        StatusMessage = "Son dosyalar temizlendi.";
    }

    private async Task ApplyThemeAsync(ThemeOption option)
    {
        _settings.Current.Theme = option.Value;
        await _settings.SaveAsync();
        if (App.Window.Content is FrameworkElement root)
        {
            _theme.Apply(root);
        }
    }
}

public sealed record ThemeOption(ThemePreference Value, string Label);
