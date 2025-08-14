using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Desktop.Configuration;
using Business.Services;

namespace Desktop.ViewModels;

public class BuildConfirmationDialogViewModel : ViewModelBase
{
    private readonly ApplicationOptions _applicationOptions;
    private readonly IMarkdownFileCollectorService _fileCollectorService;
    private readonly IMarkdownCombinationService _combinationService;
    private readonly IMarkdownDocumentFileWriterService _fileWriterService;
    private readonly ILogger<BuildConfirmationDialogViewModel> _logger;
    private bool _isBuildInProgress;
    private string _buildStatus = string.Empty;

    public BuildConfirmationDialogViewModel(
        IOptions<ApplicationOptions> applicationOptions,
        IMarkdownFileCollectorService fileCollectorService,
        IMarkdownCombinationService combinationService,
        IMarkdownDocumentFileWriterService fileWriterService,
        ILogger<BuildConfirmationDialogViewModel> logger)
    {
        _applicationOptions = applicationOptions.Value;
        _fileCollectorService = fileCollectorService;
        _combinationService = combinationService;
        _fileWriterService = fileWriterService;
        _logger = logger;
        
        CancelCommand = new RelayCommand(OnCancel);
        SaveCommand = new RelayCommand(OnSave, CanSave);
    }

    public string OutputLocation => Path.Combine(_applicationOptions.DefaultProjectFolder, _applicationOptions.DefaultOutputFolder);

    public bool IsBuildInProgress
    {
        get => _isBuildInProgress;
        set
        {
            if (SetProperty(ref _isBuildInProgress, value))
            {
                OnPropertyChanged(nameof(CanBuild));
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string BuildStatus
    {
        get => _buildStatus;
        set => SetProperty(ref _buildStatus, value);
    }

    public bool CanBuild => !IsBuildInProgress;

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }

    public event EventHandler? DialogClosed;

    private void OnCancel()
    {
        DialogClosed?.Invoke(this, EventArgs.Empty);
    }

    private async void OnSave()
    {
        await BuildDocumentationAsync();
        DialogClosed?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSave()
    {
        return CanBuild;
    }

    private async Task BuildDocumentationAsync()
    {
        try
        {
            IsBuildInProgress = true;
            BuildStatus = "Starting build...";
            
            _logger.LogInformation("Starting documentation build from: {ProjectFolder}", _applicationOptions.DefaultProjectFolder);
            
            BuildStatus = "Collecting markdown files...";
            var (templateFiles, sourceFiles) = await _fileCollectorService.CollectAllMarkdownFilesAsync(_applicationOptions.DefaultProjectFolder);
            
            _logger.LogInformation("Found {TemplateCount} template files and {SourceCount} source files", 
                templateFiles.Count(), sourceFiles.Count());
            
            BuildStatus = "Processing templates...";
            var processedDocuments = _combinationService.BuildDocumentation(templateFiles, sourceFiles);
            
            BuildStatus = "Writing output files...";
            await _fileWriterService.WriteDocumentsToFolderAsync(processedDocuments, OutputLocation);
            
            BuildStatus = "Build completed successfully!";
            _logger.LogInformation("Documentation build completed successfully. Output written to: {OutputLocation}", OutputLocation);
        }
        catch (Exception ex)
        {
            BuildStatus = $"Build failed: {ex.Message}";
            _logger.LogError(ex, "Error during documentation build");
        }
        finally
        {
            IsBuildInProgress = false;
        }
    }
}