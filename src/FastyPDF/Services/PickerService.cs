using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace FastyPDF.Services;

public sealed class PickerService
{
    public async Task<IReadOnlyList<string>> PickPdfFilesAsync(bool multiple)
    {
        var picker = new FileOpenPicker();
        Initialize(picker);
        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");
        if (multiple)
        {
            var files = await picker.PickMultipleFilesAsync();
            return files?.Select(file => file.Path).ToList() ?? [];
        }

        var file = await picker.PickSingleFileAsync();
        return file is null ? [] : [file.Path];
    }

    public async Task<IReadOnlyList<string>> PickImageFilesAsync()
    {
        var picker = new FileOpenPicker();
        Initialize(picker);
        picker.ViewMode = PickerViewMode.Thumbnail;
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".bmp", ".webp", ".tif", ".tiff" })
        {
            picker.FileTypeFilter.Add(ext);
        }

        var files = await picker.PickMultipleFilesAsync();
        return files?.Select(file => file.Path).ToList() ?? [];
    }

    public async Task<string?> PickSavePdfAsync(string suggestedName)
    {
        var picker = new FileSavePicker();
        Initialize(picker);
        picker.SuggestedFileName = suggestedName;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("PDF", [".pdf"]);
        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        Initialize(picker);
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public static IEnumerable<string> FilterDroppedFiles(IReadOnlyList<IStorageItem> items, IEnumerable<string> extensions)
    {
        var allowed = extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return items
            .OfType<StorageFile>()
            .Where(file => allowed.Contains(file.FileType))
            .Select(file => file.Path);
    }

    private static void Initialize(object picker)
    {
        InitializeWithWindow.Initialize(picker, App.WindowHandle);
    }
}
