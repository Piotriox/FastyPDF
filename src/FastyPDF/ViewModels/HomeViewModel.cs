using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastyPDF.Core.Abstractions;
using FastyPDF.Navigation;

namespace FastyPDF.ViewModels;

public partial class ToolCardViewModel : ObservableObject
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public required string Glyph { get; init; }
}

public partial class HomeViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IRecentFilesService _recentFiles;

    public HomeViewModel(INavigationService navigation, IRecentFilesService recentFiles)
    {
        _navigation = navigation;
        _recentFiles = recentFiles;
        Tools =
        [
            new ToolCardViewModel { Id = ToolIds.Merge, Title = "PDF Birleştir", Description = "Birden fazla PDF'yi tek belgede birleştirin.", Glyph = "\uE8C6" },
            new ToolCardViewModel { Id = ToolIds.Split, Title = "PDF Ayır", Description = "Sayfaları veya aralıkları ayrı PDF'lere çıkarın.", Glyph = "\uE8C7" },
            new ToolCardViewModel { Id = ToolIds.Organize, Title = "Sayfa Sıralama", Description = "Sayfaları sürükleyin, silin ve yeniden sıralayın.", Glyph = "\uE8FD" },
            new ToolCardViewModel { Id = ToolIds.Reader, Title = "PDF Okuyucu", Description = "PDF dosyalarını hızlıca görüntüleyin.", Glyph = "\uE8A5" },
            new ToolCardViewModel { Id = ToolIds.ImageToPdf, Title = "Resim → PDF", Description = "Görsellerden yeni bir PDF oluşturun.", Glyph = "\uEB9F" },
            new ToolCardViewModel { Id = ToolIds.PdfToImage, Title = "PDF → Resim", Description = "Sayfaları PNG veya JPEG olarak kaydedin.", Glyph = "\uE91B" }
        ];
    }

    public IReadOnlyList<ToolCardViewModel> Tools { get; }

    public ObservableCollection<Core.Models.RecentFileEntry> RecentFiles { get; } = [];

    public bool HasRecentFiles => RecentFiles.Count > 0;

    public async Task RefreshAsync()
    {
        await _recentFiles.LoadAsync();
        RecentFiles.Clear();
        foreach (var item in _recentFiles.Items)
        {
            RecentFiles.Add(item);
        }

        OnPropertyChanged(nameof(HasRecentFiles));
    }

    [RelayCommand]
    private void OpenTool(ToolCardViewModel? tool)
    {
        if (tool is null)
        {
            return;
        }

        _navigation.Navigate(tool.Id);
    }

    [RelayCommand]
    private void OpenRecent(Core.Models.RecentFileEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        _navigation.Navigate(ToolIds.Reader, entry.Path);
    }
}
