using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Desktop.Configuration;
using Business.Services;
using Business.Models;

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
    private bool _cleanOld = false;

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

    public string BuildStatus
    {
        get => _buildStatus;
        set => SetProperty(ref _buildStatus, value);
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
            BuildStatus = "Starting compile...";

            _logger.LogInformation("Starting documentation build from: {ProjectFolder}", _applicationOptions.DefaultProjectFolder);

            BuildStatus = "Collecting markdown files...";
            var (templateFiles, sourceFiles) = await _fileCollectorService.CollectAllMarkdownFilesAsync(_applicationOptions.DefaultProjectFolder);

            _logger.LogInformation("Found {TemplateCount} template files and {SourceCount} source files",
                templateFiles.Count(), sourceFiles.Count());

            BuildStatus = "Validating templates...";
            var validationErrors = await ValidateTemplatesAsync(templateFiles, sourceFiles);

            if (validationErrors.Any())
            {
                BuildStatus = $"Compile failed: {validationErrors.Count} validation errors found.";
                _logger.LogError("Build aborted due to validation errors. Fix the errors and try again.");
                return;
            }

            BuildStatus = "Processing templates...";
            var processedDocuments = _combinationService.BuildDocumentation(templateFiles, sourceFiles);

            BuildStatus = "Writing output files...";
            await _fileWriterService.WriteDocumentsToFolderAsync(processedDocuments, OutputLocation);

            BuildStatus = "Compile completed successfully!";
            _logger.LogInformation("Documentation build completed successfully. Output written to: {OutputLocation}", OutputLocation);
        }
        catch (Exception ex)
        {
            BuildStatus = $"Compile failed: {ex.Message}";
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
            // Use the new ValidateAll method for better efficiency
            combinedValidationResult = _combinationService.Validate(templateFiles, sourceFiles);

            // Create error list for build failure checking
            foreach (var error in combinedValidationResult.Errors)
            {
                var errorMessage = $"{error.LineNumber} - {error.Message}";
                validationErrors.Add(errorMessage);
                _logger.LogError("Validation error: {ErrorMessage}", error.Message);
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