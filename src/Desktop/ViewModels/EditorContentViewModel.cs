using System;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Desktop.Configuration;
using Desktop.Models;
using Desktop.Services;
using Business.Services;
using Business.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Desktop.ViewModels;

public class EditorContentViewModel : ViewModelBase
{
    private readonly ILogger<EditorContentViewModel> _logger;
    private readonly IEditorStateService _editorStateService;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMarkdownCombinationService _markdownCombinationService;
    private readonly IMarkdownFileCollectorService _markdownFileCollectorService;

    public EditorContentViewModel(
        ILogger<EditorContentViewModel> logger,
        IEditorStateService editorStateService,
        IOptions<ApplicationOptions> applicationOptions,
        IServiceProvider serviceProvider,
        IMarkdownCombinationService markdownCombinationService,
        IMarkdownFileCollectorService markdownFileCollectorService)
    {
        _logger = logger;
        _editorStateService = editorStateService;
        _applicationOptions = applicationOptions.Value;
        _serviceProvider = serviceProvider;
        _markdownCombinationService = markdownCombinationService;
        _markdownFileCollectorService = markdownFileCollectorService;

        ValidateCommand = new RelayCommand(ValidateDocumentation, CanValidateDocumentation);
        ValidateAllCommand = new RelayCommand(ValidateAllDocumentation, CanValidateAllDocumentation);
        BuildDocumentationCommand = new RelayCommand(BuildDocumentation, CanBuildDocumentation);

        // Subscribe to state changes
        _editorStateService.ActiveTabChanged += OnActiveTabChanged;
        _editorStateService.ValidationResultChanged += OnValidationResultChanged;
    }

    public string? ActiveFileContent => _editorStateService.ActiveFileContent;
    public string? ActiveFileName => _editorStateService.ActiveFileName;
    public ValidationResult? CurrentValidationResult => _editorStateService.CurrentValidationResult;
    public EditorTabViewModel? ActiveTab => _editorStateService.ActiveTab;
    public bool IsActiveTabSettings => ActiveTab?.TabType == TabType.Settings;

    public ICommand ValidateCommand { get; }
    public ICommand ValidateAllCommand { get; }
    public ICommand BuildDocumentationCommand { get; }

    public event EventHandler<BuildConfirmationDialogViewModel>? ShowBuildConfirmationDialog;

    private void OnActiveTabChanged(object? sender, EditorTabViewModel? activeTab)
    {
        OnPropertyChanged(nameof(ActiveFileContent));
        OnPropertyChanged(nameof(ActiveFileName));
        OnPropertyChanged(nameof(ActiveTab));
        OnPropertyChanged(nameof(IsActiveTabSettings));
        
        // Update command states
        ((RelayCommand)ValidateCommand).RaiseCanExecuteChanged();
    }

    private void OnValidationResultChanged(object? sender, ValidationResult? validationResult)
    {
        OnPropertyChanged(nameof(CurrentValidationResult));
    }

    private async void ValidateDocumentation()
    {
        var activeTab = ActiveTab;
        if (activeTab == null)
        {
            _logger.LogWarning("Validate requested but no active file");
            return;
        }
        
        _logger.LogInformation("Validating file: {FilePath}", activeTab.FilePath);

        try
        {
            // Get the directory containing the file to find source documents
            var fileDirectory = System.IO.Path.GetDirectoryName(activeTab.FilePath);
            if (string.IsNullOrEmpty(fileDirectory))
            {
                _logger.LogError("Could not determine directory for file: {FilePath}", activeTab.FilePath);
                return;
            }

            // Create MarkdownDocument for the active file
            var activeFileContent = activeTab.Content ?? string.Empty;
            var fileName = System.IO.Path.GetFileName(activeTab.FilePath);
            var templateDocument = new MarkdownDocument
            {
                FileName = fileName,
                FilePath = activeTab.FilePath,
                Content = activeFileContent
            };

            // Collect source documents from the same directory
            var sourceDocuments = await _markdownFileCollectorService.CollectSourceFilesAsync(fileDirectory);
            
            // Validate the template document
            var validationResult = _markdownCombinationService.Validate(templateDocument, sourceDocuments);
            
            // Store validation results for UI highlighting
            _editorStateService.CurrentValidationResult = validationResult;
            
            // Log validation results
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Validation successful for file: {FilePath}", activeTab.FilePath);
            }
            else
            {
                _logger.LogWarning("Validation failed for file: {FilePath}. Found {ErrorCount} errors and {WarningCount} warnings.", 
                    activeTab.FilePath, validationResult.Errors.Count, validationResult.Warnings.Count);
                    
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError("Validation error: {Message} at line {LineNumber}", error.Message, error.LineNumber);
                }
                
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogWarning("Validation warning: {Message} at line {LineNumber}", warning.Message, warning.LineNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of file: {FilePath}", activeTab.FilePath);
        }
    }

    private bool CanValidateDocumentation()
    {
        return ActiveTab != null;
    }

    private async void ValidateAllDocumentation()
    {
        _logger.LogInformation("Validate all documentation requested");

        try
        {
            // Collect all template and source files from the project directory
            var (templateFiles, sourceFiles) = await _markdownFileCollectorService.CollectAllMarkdownFilesAsync(_applicationOptions.DefaultProjectFolder);
            
            if (!templateFiles.Any())
            {
                _logger.LogWarning("No template files found for validation");
                return;
            }

            _logger.LogInformation("Validating {TemplateCount} template files and {SourceCount} source files", 
                templateFiles.Count(), sourceFiles.Count());
            
            // Validate all templates
            var validationResult = _markdownCombinationService.ValidateAll(templateFiles, sourceFiles);
            
            // Store validation results for UI highlighting
            _editorStateService.CurrentValidationResult = validationResult;
            
            // Log validation results
            if (validationResult.IsValid)
            {
                _logger.LogInformation("Validation successful for all {TemplateCount} template files", templateFiles.Count());
            }
            else
            {
                _logger.LogWarning("Validation failed. Found {ErrorCount} errors and {WarningCount} warnings across all template files.", 
                    validationResult.Errors.Count, validationResult.Warnings.Count);
                    
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError("Validation error: {Message} at line {LineNumber}", error.Message, error.LineNumber);
                }
                
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogWarning("Validation warning: {Message} at line {LineNumber}", warning.Message, warning.LineNumber);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during validation of all documentation files");
        }
    }

    private bool CanValidateAllDocumentation()
    {
        return true; // Can always validate all files
    }

    private void BuildDocumentation()
    {
        _logger.LogInformation("Build documentation requested");
        
        var dialogViewModel = _serviceProvider.GetRequiredService<BuildConfirmationDialogViewModel>();
        ShowBuildConfirmationDialog?.Invoke(this, dialogViewModel);
    }

    private bool CanBuildDocumentation()
    {
        return true;
    }
}