using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Desktop.Configuration;
using Desktop.Models;
using Desktop.Services;
using Business.Services;
using Business.Models;

namespace Desktop.ViewModels;

public class EditorContentViewModel : ViewModelBase
{
    private readonly ILogger<EditorContentViewModel> _logger;
    private readonly IEditorStateService _editorStateService;
    private readonly ApplicationOptions _applicationOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMarkdownCombinationService _markdownCombinationService;
    private readonly IMarkdownFileCollectorService _markdownFileCollectorService;
    private readonly IMarkdownRenderingService _markdownRenderingService;

    public EditorContentViewModel(
        ILogger<EditorContentViewModel> logger,
        IEditorStateService editorStateService,
        IOptions<ApplicationOptions> applicationOptions,
        IServiceProvider serviceProvider,
        IMarkdownCombinationService markdownCombinationService,
        IMarkdownFileCollectorService markdownFileCollectorService,
        IMarkdownRenderingService markdownRenderingService)
    {
        _logger = logger;
        _editorStateService = editorStateService;
        _applicationOptions = applicationOptions.Value;
        _serviceProvider = serviceProvider;
        _markdownCombinationService = markdownCombinationService;
        _markdownFileCollectorService = markdownFileCollectorService;
        _markdownRenderingService = markdownRenderingService;

        ValidateCommand = new RelayCommand(ValidateDocumentation, CanValidateDocumentation);
        ValidateAllCommand = new RelayCommand(ValidateAllDocumentation, CanValidateAllDocumentation);
        BuildDocumentationCommand = new RelayCommand(BuildDocumentation, CanBuildDocumentation);

        // Subscribe to state changes
        _editorStateService.ActiveTabChanged += OnActiveTabChanged;
        _editorStateService.ValidationResultChanged += OnValidationResultChanged;
    }

    public string? ActiveFileContent => _editorStateService.ActiveFileContent;
    public string? ActiveFileName => _editorStateService.ActiveFileName;
    public string? ActiveFilePath => _editorStateService.ActiveTab?.FilePath;
    public ValidationResult? CurrentValidationResult => _editorStateService.CurrentValidationResult;
    public EditorTabViewModel? ActiveTab => _editorStateService.ActiveTab;
    public bool IsActiveTabSettings => ActiveTab?.TabType == TabType.Settings;
    
    public EditorContentData? CurrentContentData
    {
        get
        {
            var activeTab = ActiveTab;
            if (activeTab == null) return null;

            return activeTab.TabType switch
            {
                TabType.File => new FileEditorContentData
                {
                    ContentType = EditorContentType.File,
                    ActiveTab = activeTab,
                    CurrentValidationResult = CurrentValidationResult,
                    ActiveFilePath = ActiveFilePath
                },
                TabType.Settings => new SettingsEditorContentData
                {
                    ContentType = EditorContentType.Settings,
                    SettingsViewModel = new SettingsContentViewModel(_serviceProvider.GetRequiredService<ILogger<SettingsContentViewModel>>())
                    {
                        ApplicationOptions = _applicationOptions
                    }
                },
                TabType.Preview => CreatePreviewContentData(activeTab),
                // Future content types can be added here:
                // TabType.Welcome => new WelcomeEditorContentData { ... },
                _ => null
            };
        }
    }

    public ICommand ValidateCommand { get; }
    public ICommand ValidateAllCommand { get; }
    public ICommand BuildDocumentationCommand { get; }

    public event EventHandler<BuildConfirmationDialogViewModel>? ShowBuildConfirmationDialog;

    private void OnActiveTabChanged(object? sender, EditorTabViewModel? activeTab)
    {
        OnPropertyChanged(nameof(ActiveFileContent));
        OnPropertyChanged(nameof(ActiveFileName));
        OnPropertyChanged(nameof(ActiveFilePath));
        OnPropertyChanged(nameof(ActiveTab));
        OnPropertyChanged(nameof(IsActiveTabSettings));
        OnPropertyChanged(nameof(CurrentContentData));
        
        // Update command states
        ((RelayCommand)ValidateCommand).RaiseCanExecuteChanged();
    }

    private void OnValidationResultChanged(object? sender, ValidationResult? validationResult)
    {
        OnPropertyChanged(nameof(CurrentValidationResult));
        OnPropertyChanged(nameof(CurrentContentData));
    }

    private async void ValidateDocumentation()
    {
        var activeTab = ActiveTab;
        if (activeTab == null || activeTab.FilePath == null)
        {
            _logger.LogWarning("Validate requested but no active file or file path");
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
            var validationResult = _markdownCombinationService.Validate([templateDocument], sourceDocuments);
            
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
        return ActiveTab != null && ActiveTab.FilePath != null;
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
            var validationResult = _markdownCombinationService.Validate(templateFiles, sourceFiles);
            
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
                    _logger.LogError("Validation error: {Message} at line {LineNumber} in file {SourceFile}", error.Message, error.LineNumber, error.SourceFile);
                }
                
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogWarning("Validation warning: {Message} at line {LineNumber} in file {SourceFile}", warning.Message, warning.LineNumber, warning.SourceFile);
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

    private FilePreviewContentData CreatePreviewContentData(EditorTabViewModel activeTab)
    {
        var filePath = activeTab.FilePath ?? string.Empty;
        var fileName = System.IO.Path.GetFileName(filePath);
        var rawContent = activeTab.Content ?? string.Empty;

        _logger.LogDebug("Creating preview content for file: {FilePath}", filePath);

        var previewData = new FilePreviewContentData
        {
            ContentType = EditorContentType.Preview,
            FilePath = filePath,
            FileContent = rawContent,
            FileName = fileName,
            IsCompiled = false
        };

        try
        {
            var compiledContent = CompileMarkdownTemplate(filePath, rawContent);
            previewData.CompiledContent = compiledContent;
            previewData.IsCompiled = true;
            
            // Convert the compiled markdown to HTML for display
            previewData.HtmlContent = $"<body>{_markdownRenderingService.ConvertToHtml(compiledContent)}</body>";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compiling markdown template: {FilePath}", filePath);
            previewData.CompilationError = ex.Message;
            previewData.IsCompiled = false;
        }

        _logger.LogDebug("Preview content created for file: {FilePath}, IsCompiled: {IsCompiled}, Html: {Html}", 
            filePath, previewData.IsCompiled, previewData.HtmlContent);

        return previewData;
    }

    private string CompileMarkdownTemplate(string filePath, string content)
    {
        // Get the directory containing the file to find source documents
        var fileDirectory = System.IO.Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(fileDirectory))
        {
            throw new InvalidOperationException($"Could not determine directory for file: {filePath}");
        }

        // Create MarkdownDocument for the template
        var templateDocument = new Business.Models.MarkdownDocument
        {
            FileName = System.IO.Path.GetFileName(filePath),
            FilePath = filePath,
            Content = content
        };

        // Collect source documents from the same directory (synchronously for preview)
        var sourceDocuments = Task.Run(async () => 
            await _markdownFileCollectorService.CollectSourceFilesAsync(fileDirectory)).Result;

        // Compile the template with source documents
        var compiledDocuments = _markdownCombinationService.BuildDocumentation([templateDocument], sourceDocuments);
        var compiledDocument = compiledDocuments.FirstOrDefault();

        return compiledDocument?.Content ?? content; // Fallback to original content if compilation fails
    }
}