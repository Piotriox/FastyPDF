using FastyPDF.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FastyPDF;

public sealed partial class MainWindow : Window
{
    private readonly INavigationService _navigation;
    private bool _ready;
    private bool _isUpdatingSelection;

    public MainWindow(INavigationService navigation)
    {
        _navigation = navigation;
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.SetIcon("Assets/AppIcon.ico");
        _navigation.Initialize(ContentFrame);
        _navigation.Navigated += Navigation_Navigated;
        _ready = true;
        _navigation.Navigate(ToolIds.Home);
    }

    private void Navigation_Navigated(object? sender, string toolId)
    {
        SyncNavSelection(toolId);
        NavView.IsBackEnabled = _navigation.CanGoBack;
    }

    private void SyncNavSelection(string toolId)
    {
        _isUpdatingSelection = true;
        try
        {
            if (toolId == ToolIds.Settings)
            {
                NavView.SelectedItem = NavView.SettingsItem;
                return;
            }

            var item = FindNavItem(toolId);
            NavView.SelectedItem = item;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private NavigationViewItem? FindNavItem(string tag)
    {
        foreach (var item in NavView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag as string == tag)
            {
                return navItem;
            }
        }

        foreach (var item in NavView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag as string == tag)
            {
                return navItem;
            }
        }

        return null;
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (!_ready)
        {
            return;
        }

        if (args.IsSettingsInvoked)
        {
            _navigation.Navigate(ToolIds.Settings);
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem { Tag: string tag })
        {
            _navigation.Navigate(tag);
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_ready || _isUpdatingSelection)
        {
            return;
        }

        if (args.IsSettingsSelected)
        {
            if (_navigation.CurrentToolId != ToolIds.Settings)
            {
                _navigation.Navigate(ToolIds.Settings);
            }
            return;
        }

        if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            if (_navigation.CurrentToolId != tag)
            {
                _navigation.Navigate(tag);
            }
        }
    }

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (_navigation.CanGoBack)
        {
            _navigation.GoBack();
        }
    }
}
