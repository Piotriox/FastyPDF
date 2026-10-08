using FastyPDF.Views;
using Microsoft.UI.Xaml.Controls;

namespace FastyPDF.Navigation;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

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

        _frame.Navigate(pageType, parameter);
    }
}
