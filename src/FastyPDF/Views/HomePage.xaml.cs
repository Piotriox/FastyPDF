using FastyPDF.Core.Models;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace FastyPDF.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.RefreshAsync();
    }

    private void Tools_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToolCardViewModel tool)
        {
            ViewModel.OpenToolCommand.Execute(tool);
        }
    }

    private void Recent_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is RecentFileEntry entry)
        {
            ViewModel.OpenRecentCommand.Execute(entry);
        }
    }
}
