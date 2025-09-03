using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Desktop.Configuration;
using Desktop.Services;
using Business.Services;
using Business.Models;

namespace Desktop.ViewModels;

public class BuildConfirmationDialogViewModel : ViewModelBase
{
    private readonly ApplicationOptions _applicationOptions;
    private readonly IMarkdownFileCollectorService _fileCollectorService;
    private readonly IMarkdownCombinationService _combinationService;
    private readonly IMarkdownDocumentFileWriterService _fileWriterService;
    private readonly IFileService _fileService;
    private readonly ILogger<BuildConfirmationDialogViewModel> _logger;
    private bool _isBuildInProgress;
    private bool _cleanOld = false;

    public BuildConfirmationDialogViewModel(
        IOptions<ApplicationOptions> applicationOptions,
        IMarkdownFileCollectorService fileCollectorService,
        IMarkdownCombinationService combinationService,
        IMarkdownDocumentFileWriterService fileWriterService,
        IFileService fileService,
        ILogger<BuildConfirmationDialogViewModel> logger)
    {
        _applicationOptions = applicationOptions.Value;
        _fileCollectorService = fileCollectorService;
        _combinationService = combinationService;
        _fileWriterService = fileWriterService;
        _fileService = fileService;
        _logger = logger;

        CancelCommand = new RelayCommand(OnCancel);
        SaveCommand = new RelayCommand(OnSave, CanSave);
    }

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }

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


    public bool CanBuild => !IsBuildInProgress;

    public bool CleanOld
    {
        get => _cleanOld;
        set => SetProperty(ref _cleanOld, value);
    }

    public event EventHandler? DialogClosed;
    public event EventHandler<ValidationResult>? ValidationResultsAvailable;

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

            _logger.LogInformation("Starting documentation build from: {ProjectFolder}", _applicationOptions.DefaultProjectFolder);

            var (templateFiles, sourceFiles) = await _fileCollectorService.CollectAllMarkdownFilesAsync(_applicationOptions.DefaultProjectFolder);

            _logger.LogInformation("Found {TemplateCount} template files and {SourceCount} source files",
                templateFiles.Count(), sourceFiles.Count());

            var validationErrors = await ValidateTemplatesAsync(templateFiles, sourceFiles);

            if (validationErrors.Any())
            {
                _logger.LogError("Build aborted due to validation errors. Fix the errors and try again.");
                return;
            }

            var processedDocuments = _combinationService.BuildDocumentation(templateFiles, sourceFiles);

            if (CleanOld)
            {
                _logger.LogInformation("Cleaning output folder: {OutputLocation}", OutputLocation);

                var cleanSuccess = await _fileService.DeleteFolderContentsAsync(OutputLocation);
                if (!cleanSuccess)
                {
                    _logger.LogWarning("Failed to clean output folder, continuing with build");
                }
            }

            await _fileWriterService.WriteDocumentsToFolderAsync(processedDocuments, OutputLocation);

            _logger.LogInformation("Documentation build completed successfully. Output written to: {OutputLocation}", OutputLocation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during documentation build");
        }
        finally
        {
            IsBuildInProgress = false;
        }
    }

    private async Task<List<string>> ValidateTemplatesAsync(IEnumerable<MarkdownDocument> templateFiles, IEnumerable<MarkdownDocument> sourceFiles)
    {
        var validationErrors = new List<string>();

        ValidationResult combinedValidationResult = null!;

        await Task.Run(() =>
        {
            combinedValidationResult = _combinationService.Validate(templateFiles, sourceFiles);

            // Create error list for build failure checking
            foreach (var error in combinedValidationResult.Errors)
            {
                var errorMessage = $"{error.LineNumber} - {error.Message}";
                validationErrors.Add(errorMessage);
                _logger.LogError("Validation error: {ErrorMessage}", errorMessage);
            }

            foreach (var warning in combinedValidationResult.Warnings)
            {
                _logger.LogWarning("Validation warning: {WarningMessage}", warning.Message);
            }
        });

        // Emit validation results for the error view
        ValidationResultsAvailable?.Invoke(this, combinedValidationResult);

        if (validationErrors.Any())
        {
            _logger.LogError("Validation completed with {ErrorCount} errors", validationErrors.Count);
        }
        else
        {
            _logger.LogInformation("Validation completed successfully with no errors");
        }

        return validationErrors;
    }
}