using CommunityToolkit.Mvvm.ComponentModel;
using FastyPDF.Core.Exceptions;
using FastyPDF.Core.Models;

namespace FastyPDF.ViewModels;

public abstract partial class ToolViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private bool _isProgressIndeterminate = true;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isErrorOpen;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isSuccessOpen;

    protected IProgress<ProgressUpdate> CreateProgress()
    {
        return new Progress<ProgressUpdate>(update =>
        {
            StatusMessage = update.Message;
            if (update.Percent is { } percent)
            {
                IsProgressIndeterminate = false;
                ProgressValue = percent;
            }
            else
            {
                IsProgressIndeterminate = true;
            }
        });
    }

    protected void ShowError(Exception exception)
    {
        var message = exception is PdfOperationException pdf
            ? pdf.UserMessage
            : (!string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "Beklenmeyen bir hata oluştu.");
        ErrorMessage = message;
        IsErrorOpen = true;
        IsSuccessOpen = false;
    }

    protected void ShowSuccess(string message)
    {
        SuccessMessage = message;
        IsSuccessOpen = true;
        IsErrorOpen = false;
    }

    protected void ClearMessages()
    {
        IsErrorOpen = false;
        IsSuccessOpen = false;
        ErrorMessage = null;
        SuccessMessage = null;
    }
}
