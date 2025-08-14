using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Desktop.Configuration;

namespace Desktop.ViewModels;

public class BuildConfirmationDialogViewModel : ViewModelBase
{
    private readonly ApplicationOptions _applicationOptions;

    public BuildConfirmationDialogViewModel(IOptions<ApplicationOptions> applicationOptions)
    {
        _applicationOptions = applicationOptions.Value;
        
        CancelCommand = new RelayCommand(OnCancel);
        SaveCommand = new RelayCommand(OnSave, CanSave);
    }

    public string OutputLocation => Path.Combine(_applicationOptions.DefaultProjectFolder, _applicationOptions.DefaultOutputFolder);

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }

    public event EventHandler? DialogClosed;

    private void OnCancel()
    {
        DialogClosed?.Invoke(this, EventArgs.Empty);
    }

    private void OnSave()
    {
        DialogClosed?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave()
    {
        return false;
    }
}