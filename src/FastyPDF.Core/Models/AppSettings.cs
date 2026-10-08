namespace FastyPDF.Core.Models;

public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2
}

public sealed class AppSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    public string? DefaultOutputFolder { get; set; }

    public int RecentFileLimit { get; set; } = 10;

    public int JpegQuality { get; set; } = 92;

    public int ThumbnailWidth { get; set; } = 160;
}
