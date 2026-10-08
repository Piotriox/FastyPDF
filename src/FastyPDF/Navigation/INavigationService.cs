namespace FastyPDF.Navigation;

public interface INavigationService
{
    void Initialize(Microsoft.UI.Xaml.Controls.Frame frame);

    void Navigate(string toolId, object? parameter = null);
}
