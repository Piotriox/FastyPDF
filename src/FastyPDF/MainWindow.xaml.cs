using FastyPDF.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FastyPDF;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigation;
    private bool _ready;

    public MainWindow(INavigationService navigation)
    {
        _navigation = navigation;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");
        _navigation.Initialize(ContentFrame);
        _ready = true;
        NavView.SelectedItem = NavView.MenuItems[0];
        _navigation.Navigate(ToolIds.Home);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_ready)
        {
            return;
        }

        if (args.IsSettingsSelected)
        {
            _navigation.Navigate(ToolIds.Settings);
            return;
        }

        if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            _navigation.Navigate(tag);
        }
    }
}
