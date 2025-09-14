using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using ProjectDocumentationManager.Business.Models;
using ProjectDocumentationManager.Business.Services;

namespace ProjectDocumentationManager.Business.Tests.Services;

[TestFixture]
public class MarkdownCombinationServiceTests
{
    private ILogger<MarkdownCombinationService> _mockLogger;
    private MarkdownCombinationService _service;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = NullLoggerFactory.Instance.CreateLogger<MarkdownCombinationService>();
        _service = new MarkdownCombinationService(_mockLogger);
    }

    [Test]
    public void BuildDocumentation_WithNullDocuments_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.BuildDocumentation(null!));
    }

    [Test]
    public void BuildDocumentation_WithEmptyDocuments_ReturnsEmptyList()
    {
        // Arrange
        var documents = new List<MarkdownDocument>();

        // Act
        var result = _service.BuildDocumentation(documents);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.Zero);
    }

    [Test]
    public void BuildDocumentation_WithTemplateWithoutInserts_ReturnsUnchangedTemplate()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\nThis is regular content without inserts." }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].FileName, Is.EqualTo("template.md"));
            Assert.That(result[0].Content, Is.EqualTo("# Title\n\nThis is regular content without inserts."));
        }
    }

    [Test]
    public void BuildDocumentation_WithBasicInsert_ReplacesInsertDirective()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "windows-features.mdext", FilePath = "/test/windows-features.mdext", Content = " * windows feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.mdsrc\" />" },
            new() { FileName = "common-features.mdsrc", FilePath = "/test/common-features.mdsrc", Content = " * common feature" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].FileName, Is.EqualTo("windows-features.md"));
            Assert.That(result[0].Content, Is.EqualTo(" * windows feature\n  * common feature"));
        });
    }

    [Test]
    public void BuildDocumentation_WithMultipleInserts_ReplacesAllDirectives()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "combined.mdext", FilePath = "/test/combined.mdext", Content = "# Features\n\n<MarkDownExtension operation=\"insert\" file=\"intro.mdsrc\" />\n\n<MarkDownExtension operation=\"insert\" file=\"details.mdsrc\" />\n\n<MarkDownExtension operation=\"insert\" file=\"conclusion.mdsrc\" />" },
            new() { FileName = "intro.mdsrc", FilePath = "/test/intro.mdsrc", Content = "This is the introduction." },
            new() { FileName = "details.mdsrc", FilePath = "/test/details.mdsrc", Content = "Here are the details." },
            new() { FileName = "conclusion.mdsrc", FilePath = "/test/conclusion.mdsrc", Content = "This is the conclusion." }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var processedContent = result[0].Content;
        Assert.That(processedContent, Does.Contain("This is the introduction."));
        Assert.That(processedContent, Does.Contain("Here are the details."));
        Assert.That(processedContent, Does.Contain("This is the conclusion."));
        Assert.That(processedContent, Does.Not.Contain("<MarkDownExtension"));
    }

    [Test]
    public void BuildDocumentation_WithMultipleTemplates_ProcessesAllTemplates()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "windows-features.mdext", FilePath = "/test/windows-features.mdext", Content = " * windows feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.mdsrc\" />" },
            new() { FileName = "ubuntu-features.mdext", FilePath = "/test/ubuntu-features.mdext", Content = " * linux feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.mdsrc\" />" },
            new() { FileName = "common-features.mdsrc", FilePath = "/test/common-features.mdsrc", Content = " * common feature" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        var windowsResult = result.First(r => r.FileName == "windows-features.md");
        Assert.That(windowsResult.Content, Is.EqualTo(" * windows feature\n  * common feature"));

        var ubuntuResult = result.First(r => r.FileName == "ubuntu-features.md");
        Assert.That(ubuntuResult.Content, Is.EqualTo(" * linux feature\n  * common feature"));
    }

    [Test]
    public void BuildDocumentation_WithMissingSourceDocument_ReplacesWithComment()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"missing-file.mdsrc\" />\n\nEnd." }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Does.Contain("<!-- Missing source: missing-file.mdsrc -->"));
        Assert.That(result[0].Content, Does.Not.Contain("<MarkDownExtension operation=\"insert\" file=\"missing-file.mdsrc\" />"));
    }

    [Test]
    public void BuildDocumentation_WithNestedInserts_ProcessesRecursively()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "main.mdext", FilePath = "/test/main.mdext", Content = "# Main\n\n<MarkDownExtension operation=\"insert\" file=\"level1.mdsrc\" />" },
            new() { FileName = "level1.mdsrc", FilePath = "/test/level1.mdsrc", Content = "## Level 1\n\n<MarkDownExtension operation=\"insert\" file=\"level2.mdsrc\" />" },
            new() { FileName = "level2.mdsrc", FilePath = "/test/level2.mdsrc", Content = "### Level 2 Content" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var processedContent = result[0].Content;
        Assert.That(processedContent, Does.Contain("# Main"));
        Assert.That(processedContent, Does.Contain("## Level 1"));
        Assert.That(processedContent, Does.Contain("### Level 2 Content"));
        Assert.That(processedContent, Does.Not.Contain("<MarkDownExtension"));
    }

    [Test]
    public void BuildDocumentation_WithCaseInsensitiveFilenames_MatchesCorrectly()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "<MarkDownExtension operation=\"insert\" file=\"COMMON-FEATURES.MDSRC\" />" },
            new() { FileName = "common-features.mdsrc", FilePath = "/test/common-features.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo("Common content"));
    }

    [Test]
    public void BuildDocumentation_WithInsertDirectiveWithSpaces_ProcessesCorrectly()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "Before\n<MarkDownExtension operation=\"insert\" file=\"common-features.mdsrc\" />\nAfter" },
            new() { FileName = "common-features.mdsrc", FilePath = "/test/common-features.mdsrc", Content = "Inserted content" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo("Before\nInserted content\nAfter"));
    }

    [Test]
    public void BuildDocumentation_WithDuplicateInserts_ReplacesAllOccurrences()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "Start\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />\nMiddle\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />\nEnd" },
            new() { FileName = "common.mdsrc", FilePath = "/test/common.mdsrc", Content = "CONTENT" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var expected = "Start\nCONTENT\nMiddle\nCONTENT\nEnd";
        Assert.That(result[0].Content, Is.EqualTo(expected));
    }

    [Test]
    public void BuildDocumentation_WithEmptySourceContent_InsertsEmptyString()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "Before <MarkDownExtension operation=\"insert\" file=\"empty.mdsrc\" /> After" },
            new() { FileName = "empty.mdsrc", FilePath = "/test/empty.mdsrc", Content = "" }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo("Before  After"));
    }


    [Test]
    public void BuildDocumentation_Should_Log_Source_Documents_Without_Errors()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<MarkdownCombinationService>();
        var service = new MarkdownCombinationService(logger);

        var documents = new[]
        {
            new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Template\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />" },
            new MarkdownDocument { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Source 1 content" },
            new MarkdownDocument { FileName = "source2.mdsrc", FilePath = "/test/source2.mdsrc", Content = "Source 2 content" },
            new MarkdownDocument { FileName = "source3.mdsrc", FilePath = "/test/source3.mdsrc", Content = "Source 3 content" } // This one won't be used
        };

        // Act & Assert - Should not throw any exceptions
        Assert.DoesNotThrow(() =>
        {
            var result = service.BuildDocumentation(documents);
            var resultList = result.ToList(); // Force enumeration

            // Verify the functionality still works correctly
            Assert.That(resultList, Has.Count.EqualTo(1));
            Assert.That(resultList[0].Content, Does.Contain("Source 1 content"));
            Assert.That(resultList[0].Content, Does.Contain("Source 2 content"));
        });
    }

    [Test]
    public void BuildDocumentation_Should_Handle_Empty_Source_Documents_Without_Errors()
    {
        // Arrange
        var documents = new[]
        {
            new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Template with no inserts" }
        };

        // Act & Assert - Should not throw any exceptions
        Assert.DoesNotThrow(() =>
        {
            var result = _service.BuildDocumentation(documents);
            var resultList = result.ToList(); // Force enumeration

            // Verify the functionality still works correctly
            Assert.That(resultList, Has.Count.EqualTo(1));
            Assert.That(resultList[0].Content, Is.EqualTo("# Template with no inserts"));
        });
    }

    [Test]
    public void BuildDocumentation_Should_Change_Mdext_Extension_To_Md()
    {
        // Arrange
        var documents = new[]
        {
            new MarkdownDocument { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1" },
            new MarkdownDocument { FileName = "subfolder/template2.mdext", FilePath = "/test/subfolder/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"source.mdsrc\" />" },
            new MarkdownDocument { FileName = "source.mdsrc", FilePath = "/test/source.mdsrc", Content = "Source content" }
        };

        // Act
        var result = _service.BuildDocumentation(documents);
        var resultList = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultList, Has.Count.EqualTo(2), "Should process both templates");

            // Verify file extensions are changed to .md
            Assert.That(resultList[0].FileName, Is.EqualTo("template1.md"), "First template should have .md extension");
            Assert.That(resultList[1].FileName, Is.EqualTo("subfolder/template2.md"), "Second template should have .md extension with preserved path");

            // Verify content is processed correctly
            Assert.That(resultList[0].Content, Is.EqualTo("# Template 1"), "First template content should be unchanged");
            Assert.That(resultList[1].Content, Is.EqualTo("# Template 2\nSource content"), "Second template should have processed content");
        });
    }

    [Test]
    public void BuildDocumentation_Should_Change_Extension_To_Md_Even_On_Processing_Error()
    {
        // Arrange
        var documents = new[]
        {
            new MarkdownDocument { FileName = "error-template.mdext", FilePath = "/test/error-template.mdext", Content = "# Template\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />" }
        };

        // Act
        var result = _service.BuildDocumentation(documents);
        var resultList = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultList, Has.Count.EqualTo(1), "Should still process template despite missing source");
            Assert.That(resultList[0].FileName, Is.EqualTo("error-template.md"), "Should change extension to .md even when processing encounters missing sources");
            Assert.That(resultList[0].Content, Does.Contain("<!-- Missing source: missing.mdsrc -->"), "Should contain missing source comment");
        });
    }

    #region Validation Tests

    [Test]
    public void Validate_WithMissingSourceFile_ReturnsErrorResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template.mdext] Source document not found: 'missing.mdsrc'"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("missing.mdsrc"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Errors[0].SourceContext, Is.EqualTo("<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />"));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithEmptyFilename_ReturnsErrorResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"\" />" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template.mdext] MarkDownExtension directive is missing filename"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("<MarkDownExtension operation=\"insert\" file=\"\" />"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithInvalidFilenameCharacters_ReturnsErrorResult()
    {
        // Arrange - Use one or more invalid filename characters for the current platform
        var invalidChars = new string(Path.GetInvalidFileNameChars().Where(c => c != Path.DirectorySeparatorChar).Take(2).ToArray());
        var invalidFileName = $"invalid{invalidChars}file.mdsrc";
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = $"# Title\n\n<MarkDownExtension operation=\"insert\" file=\"{invalidFileName}\" />" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Does.StartWith("[template.mdext] MarkDownExtension directive contains invalid filename characters:"));
            Assert.That(result.Errors[0].DirectivePath, Does.Contain("invalid"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithDuplicateDirectives_ReturnsWarningResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new()
            {
                FileName = "template.mdext",
                FilePath = "/test/template.mdext",
                Content =
                    "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />\n\nSome content\n\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />"
            },
            new() { FileName = "common.mdsrc", FilePath = "/test/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Is.EqualTo("[template.mdext] Duplicate MarkDownExtension directive found: '<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />'"));
            Assert.That(result.Warnings[0].DirectivePath, Is.EqualTo("common.mdsrc"));
            Assert.That(result.Warnings[0].LineNumber, Is.EqualTo(7));
        });
    }

    [Test]
    public void Validate_WithMultipleDirectivesOnSameLine_ValidatesEach()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new()
            {
                FileName = "template.mdext",
                FilePath = "/test/template.mdext",
                Content =
                    "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" /> and <MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />"
            },
            new() { FileName = "valid.mdsrc", FilePath = "/test/valid.mdsrc", Content = "Valid content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template.mdext] Source document not found: 'missing.mdsrc'"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }


    [Test]
    public void Validate_WithCircularReference_ReturnsWarningResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"circular1.mdsrc\" />" },
            new() { FileName = "circular1.mdsrc", FilePath = "/test/circular1.mdsrc", Content = "Content <MarkDownExtension operation=\"insert\" file=\"circular2.mdsrc\" />" },
            new() { FileName = "circular2.mdsrc", FilePath = "/test/circular2.mdsrc", Content = "Content <MarkDownExtension operation=\"insert\" file=\"circular1.mdsrc\" />" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Does.StartWith("[template.mdext] Potential circular reference detected"));
        });
    }


    [Test]
    public void Validate_WithMissingOperationAttribute_ReturnsErrorResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension file=\"common/common.mdsrc\" />" },
            new() { FileName = "common/common.mdsrc", FilePath = "/test/common/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template.mdext] MarkDownExtension directive is missing 'operation' attribute"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("<MarkDownExtension file=\"common/common.mdsrc\" />"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithMissingFileAttribute_ReturnsErrorResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" />" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template.mdext] MarkDownExtension directive is missing 'file' attribute"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("<MarkDownExtension operation=\"insert\" />"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithInvalidOperationValue_ReturnsErrorResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"no-operation-like this\" />" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template.mdext] MarkDownExtension directive has invalid operation. Only 'insert' is supported"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("<MarkDownExtension operation=\"no-operation-like this\" />"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithAllErrorCasesFromBasicErrors_ReturnsAppropriateErrors()
    {
        // Arrange - This matches the content from example-projects/errors/basic-errors.mdext
        var documents = new List<MarkdownDocument>
        {
            new()
            {
                FileName = "basic-errors.mdext",
                FilePath = "/test/basic-errors.mdext",
                Content = @"# Error showcase

This file is meant to be used as a testcase for showing errors

Missing operation attribute
<MarkDownExtension file=""common/common.mdsrc"" />

Unknown operation attribute
<MarkDownExtension operation=""no-operation-like this"" />

Missing file attribute
<MarkDownExtension operation=""insert"" />

Missing file
<MarkDownExtension operation=""insert"" file=""im-not-found.mdsrc"" />"
            },
            new() { FileName = "common/common.mdsrc", FilePath = "/test/common/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(4));

            // Check for missing operation attribute error
            Assert.That(result.Errors.Any(e => e.Message.Contains("MarkDownExtension directive is missing 'operation' attribute")), Is.True);

            // Check for invalid operation error
            Assert.That(result.Errors.Any(e => e.Message.Contains("MarkDownExtension directive has invalid operation. Only 'insert' is supported")), Is.True);

            // Check for missing file attribute error
            Assert.That(result.Errors.Any(e => e.Message.Contains("MarkDownExtension directive is missing 'file' attribute")), Is.True);

            // Check for missing source file error
            Assert.That(result.Errors.Any(e => e.Message.Contains("Source document not found: 'im-not-found.mdsrc'")), Is.True);

            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithMixedValidAndInvalidDirectives_ReturnsAppropriateResults()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"\" />" },
            new() { FileName = "valid.mdsrc", FilePath = "/test/valid.mdsrc", Content = "Valid content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));

            // Check for missing file error
            Assert.That(result.Errors.Any(e => e.Message.Contains("Source document not found: 'missing.mdsrc'")), Is.True);

            // Check for empty filename error
            Assert.That(result.Errors.Any(e => e.Message.Contains("MarkDownExtension directive is missing filename")), Is.True);

            // Check for duplicate warning
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Is.EqualTo("[template.mdext] Duplicate MarkDownExtension directive found: '<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" />'"));
        });
    }

    [Test]
    public void Validate_WithNullDocuments_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.Validate(null!));
    }

    [Test]
    public void Validate_WithEmptyDocuments_ReturnsValidResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithValidTemplateDocuments_ReturnsValidResult()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />" },
            new() { FileName = "template2.mdext", FilePath = "/test/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />" },
            new() { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Content 1" },
            new() { FileName = "source2.mdsrc", FilePath = "/test/source2.mdsrc", Content = "Content 2" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithErrorsInMultipleTemplates_CombinesAllErrors()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1\n<MarkDownExtension operation=\"insert\" file=\"missing1.mdsrc\" />" },
            new() { FileName = "template2.mdext", FilePath = "/test/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"missing2.mdsrc\" />" },
            new() { FileName = "available.mdsrc", FilePath = "/test/available.mdsrc", Content = "Available content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[template1.mdext] Source document not found: 'missing1.mdsrc'"));
            Assert.That(result.Errors[1].Message, Is.EqualTo("[template2.mdext] Source document not found: 'missing2.mdsrc'"));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithMixedValidAndInvalidTemplates_ReturnsOnlyErrors()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "valid.mdext", FilePath = "/test/valid.mdext", Content = "# Valid Template\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />" },
            new() { FileName = "invalid.mdext", FilePath = "/test/invalid.mdext", Content = "# Invalid Template\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />" },
            new() { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Available content" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("[invalid.mdext] Source document not found: 'missing.mdsrc'"));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithWarningsInMultipleTemplates_CombinesAllWarnings()
    {
        // Arrange
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />" },
            new() { FileName = "template2.mdext", FilePath = "/test/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />" },
            new() { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Content 1" },
            new() { FileName = "source2.mdsrc", FilePath = "/test/source2.mdsrc", Content = "Content 2" }
        };

        // Act
        var result = _service.Validate(documents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True); // Warnings don't make it invalid
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(2));
            Assert.That(result.Warnings[0].Message, Does.StartWith("[template1.mdext] Duplicate MarkDownExtension directive found"));
            Assert.That(result.Warnings[1].Message, Does.StartWith("[template2.mdext] Duplicate MarkDownExtension directive found"));
        });
    }

    [Test]
    public void BuildDocumentation_WithMdAndMdextFilesAsInsertSources_ProcessesAllFileTypesCorrectly()
    {
        // Arrange - This test verifies that .md, .mdsrc, and .mdext files are all supported as insert sources
        var documents = new List<MarkdownDocument>
        {
            new() { FileName = "main.mdext", FilePath = "/test/main.mdext", Content = "# Main Document\n\n<MarkDownExtension operation=\"insert\" file=\"section1.md\" />\n\n<MarkDownExtension operation=\"insert\" file=\"section2.mdsrc\" />\n\n<MarkDownExtension operation=\"insert\" file=\"section3.mdext\" />\n\n## End" },
            new() { FileName = "section1.md", FilePath = "/test/section1.md", Content = "## Section 1\nThis content comes from a .md file." },
            new() { FileName = "section2.mdsrc", FilePath = "/test/section2.mdsrc", Content = "## Section 2\nThis content comes from a .mdsrc file." },
            new() { FileName = "section3.mdext", FilePath = "/test/section3.mdext", Content = "## Section 3\nThis content comes from a .mdext file.\n\n<MarkDownExtension operation=\"insert\" file=\"subsection.md\" />" },
            new() { FileName = "subsection.md", FilePath = "/test/subsection.md", Content = "### Subsection\nNested content from another .md file." }
        };

        // Act
        var result = _service.BuildDocumentation(documents).ToList();

        // Assert - All file types (.md, .mdsrc, .mdext) are treated as valid source files for inserts
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should process main template and section3.mdext template");

            var mainResult = result.First(r => r.FileName == "main.md");
            var section3Result = result.First(r => r.FileName == "section3.md");

            var processedContent = mainResult.Content;
            Assert.That(processedContent, Does.Contain("# Main Document"), "Should contain main document header");
            Assert.That(processedContent, Does.Contain("## Section 1"), "Should contain .md file content");
            Assert.That(processedContent, Does.Contain("This content comes from a .md file"), "Should include .md file content");
            Assert.That(processedContent, Does.Contain("## Section 2"), "Should contain .mdsrc content");
            Assert.That(processedContent, Does.Contain("This content comes from a .mdsrc file"), "Should include .mdsrc file content");
            Assert.That(processedContent, Does.Contain("## Section 3"), "Should contain .mdext content");
            Assert.That(processedContent, Does.Contain("This content comes from a .mdext file"), "Should include .mdext file content");
            Assert.That(processedContent, Does.Contain("### Subsection"), "Should contain nested content from .mdext source");
            Assert.That(processedContent, Does.Contain("Nested content from another .md file"), "Should include nested .md file content");
            Assert.That(processedContent, Does.Contain("## End"), "Should contain final content from main template");
            Assert.That(processedContent, Does.Not.Contain("<MarkDownExtension"), "Should not contain any unprocessed directives");

            // Verify that section3.mdext was also processed as a template
            Assert.That(section3Result.Content, Does.Contain("## Section 3"), "Section3 template should be processed");
            Assert.That(section3Result.Content, Does.Contain("### Subsection"), "Section3 template should contain nested content");
        }
    }

    #endregion
}