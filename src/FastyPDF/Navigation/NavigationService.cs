using FastyPDF.Views;
using Microsoft.UI.Xaml.Controls;

namespace FastyPDF.Navigation;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    public string? CurrentToolId { get; private set; }

    public event EventHandler<string>? Navigated;

    public bool CanGoBack => _frame?.CanGoBack == true;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void Navigate(string toolId, object? parameter = null)
    {
        if (_frame is null)
        {
            return;
        }

        var pageType = toolId switch
        {
            ToolIds.Home => typeof(HomePage),
            ToolIds.Merge => typeof(MergePage),
            ToolIds.Split => typeof(SplitPage),
            ToolIds.Organize => typeof(OrganizePage),
            ToolIds.Reader => typeof(ReaderPage),
            ToolIds.ImageToPdf => typeof(ImageToPdfPage),
            ToolIds.PdfToImage => typeof(PdfToImagePage),
            ToolIds.Settings => typeof(SettingsPage),
            ToolIds.About => typeof(AboutPage),
            _ => typeof(HomePage)
        };

        CurrentToolId = toolId;
        _frame.Navigate(pageType, parameter);
        Navigated?.Invoke(this, toolId);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
            if (_frame.CurrentSourcePageType is { } pageType)
            {
                var toolId = GetToolIdForPageType(pageType);
                CurrentToolId = toolId;
                Navigated?.Invoke(this, toolId);
            }
        }
    }

    private static string GetToolIdForPageType(Type pageType)
    {
        if (pageType == typeof(HomePage)) return ToolIds.Home;
        if (pageType == typeof(MergePage)) return ToolIds.Merge;
        if (pageType == typeof(SplitPage)) return ToolIds.Split;
        if (pageType == typeof(OrganizePage)) return ToolIds.Organize;
        if (pageType == typeof(ReaderPage)) return ToolIds.Reader;
        if (pageType == typeof(ImageToPdfPage)) return ToolIds.ImageToPdf;
        if (pageType == typeof(PdfToImagePage)) return ToolIds.PdfToImage;
        if (pageType == typeof(SettingsPage)) return ToolIds.Settings;
        if (pageType == typeof(AboutPage)) return ToolIds.About;
        return ToolIds.Home;
    }
}
