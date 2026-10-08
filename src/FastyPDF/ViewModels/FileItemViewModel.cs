using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace FastyPDF.ViewModels;

public partial class FileItemViewModel : ObservableObject
{
    public required string Path { get; init; }

    public required string Name { get; init; }

    [ObservableProperty]
    private string _details = string.Empty;

    [ObservableProperty]
    private ImageSource? _preview;
}

public partial class PageItemViewModel : ObservableObject
{
    public required int PageIndex { get; init; }

    public int DisplayNumber => PageIndex + 1;

    [ObservableProperty]
    private ImageSource? _preview;

    [ObservableProperty]
    private bool _isLoading = true;
}
