using FastyPDF.Core.Abstractions;
using FastyPDF.Core.Pdf;
using FastyPDF.Core.Services;
using FastyPDF.Navigation;
using FastyPDF.Services;
using FastyPDF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace FastyPDF;

public partial class App : Application
{
    public static Window Window { get; private set; } = null!;

    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static nint WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);

    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();
        await Services.GetRequiredService<IRecentFilesService>().LoadAsync();

        Window = Services.GetRequiredService<MainWindow>();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (Window.Content is FrameworkElement root)
        {
            Services.GetRequiredService<ThemeService>().Apply(root);
        }

        Window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppLog, FileAppLog>();
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IFileService, LocalFileService>();
        services.AddSingleton<PdfiumRuntime>();
        services.AddSingleton<IPdfDocumentService, PdfiumDocumentService>();
        services.AddSingleton<IPdfRenderService, PdfiumRenderService>();
        services.AddSingleton<IPdfManipulationService, PdfSharpManipulationService>();
        services.AddSingleton<IImageService, WicImageService>();
        services.AddSingleton<IImageEncoder, WicImageEncoder>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<PickerService>();
        services.AddSingleton<ThemeService>();

        services.AddTransient<HomeViewModel>();
        services.AddTransient<MergeViewModel>();
        services.AddTransient<SplitViewModel>();
        services.AddTransient<OrganizeViewModel>();
        services.AddTransient<ReaderViewModel>();
        services.AddTransient<ImageToPdfViewModel>();
        services.AddTransient<PdfToImageViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddSingleton<MainWindow>();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        try
        {
            Services.GetService<IAppLog>()?.Error("Unhandled exception", e.Exception);
        }
        catch
        {
            // ignored
        }
    }
}
