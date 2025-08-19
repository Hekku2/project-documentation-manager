using System;
using System.Collections.Generic;
using System.Linq;
using Business.Models;
using Business.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Business.Tests.Services;

[TestFixture]
public class MarkdownCombinationServiceTests
{
    private ILogger<MarkdownCombinationService> _mockLogger;
    private MarkdownCombinationService _service;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = Substitute.For<ILogger<MarkdownCombinationService>>();
        _service = new MarkdownCombinationService(_mockLogger);
    }

    [Test]
    public void BuildDocumentation_WithNullTemplateDocuments_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceDocuments = new List<MarkdownDocument>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.BuildDocumentation(null!, sourceDocuments));
    }

    [Test]
    public void BuildDocumentation_WithNullSourceDocuments_ThrowsArgumentNullException()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.BuildDocumentation(templateDocuments, null!));
    }

    [Test]
    public void BuildDocumentation_WithEmptyTemplates_ReturnsEmptyList()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>();
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public void BuildDocumentation_WithTemplateWithoutInserts_ReturnsUnchangedTemplate()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "# Title\n\nThis is regular content without inserts." }
        };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].FileName, Is.EqualTo("template.md"));
        Assert.That(result[0].Content, Is.EqualTo("# Title\n\nThis is regular content without inserts."));
    }

    [Test]
    public void BuildDocumentation_WithBasicInsert_ReplacesInsertDirective()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "windows-features.md", FilePath = "/test/windows-features.md", Content = " * windows feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.md\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common-features.md", FilePath = "/test/common-features.md", Content = " * common feature" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].FileName, Is.EqualTo("windows-features.md"));
        Assert.That(result[0].Content, Is.EqualTo(" * windows feature\n  * common feature"));
    }

    [Test]
    public void BuildDocumentation_WithMultipleInserts_ReplacesAllDirectives()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "combined.md", FilePath = "/test/combined.md", Content = "# Features\n\n<MarkDownExtension operation=\"insert\" file=\"intro.md\" />\n\n<MarkDownExtension operation=\"insert\" file=\"details.md\" />\n\n<MarkDownExtension operation=\"insert\" file=\"conclusion.md\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "intro.md", FilePath = "/test/intro.md", Content = "This is the introduction." },
            new MarkdownDocument { FileName = "details.md", FilePath = "/test/details.md", Content = "Here are the details." },
            new MarkdownDocument { FileName = "conclusion.md", FilePath = "/test/conclusion.md", Content = "This is the conclusion." }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

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
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "windows-features.md", FilePath = "/test/windows-features.md", Content = " * windows feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.md\" />" },
            new MarkdownDocument { FileName = "ubuntu-features.md", FilePath = "/test/ubuntu-features.md", Content = " * linux feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.md\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common-features.md", FilePath = "/test/common-features.md", Content = " * common feature" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

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
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"missing-file.md\" />\n\nEnd." }
        };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Does.Contain("<!-- Missing source: missing-file.md -->"));
        Assert.That(result[0].Content, Does.Not.Contain("<MarkDownExtension operation=\"insert\" file=\"missing-file.md\" />"));
    }

    [Test]
    public void BuildDocumentation_WithNestedInserts_ProcessesRecursively()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "main.md", FilePath = "/test/main.md", Content = "# Main\n\n<MarkDownExtension operation=\"insert\" file=\"level1.md\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "level1.md", FilePath = "/test/level1.md", Content = "## Level 1\n\n<MarkDownExtension operation=\"insert\" file=\"level2.md\" />" },
            new MarkdownDocument { FileName = "level2.md", FilePath = "/test/level2.md", Content = "### Level 2 Content" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

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
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "<MarkDownExtension operation=\"insert\" file=\"COMMON-FEATURES.MD\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common-features.md", FilePath = "/test/common-features.md", Content = "Common content" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo("Common content"));
    }

    [Test]
    public void BuildDocumentation_WithInsertDirectiveWithSpaces_ProcessesCorrectly()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "Before\n<MarkDownExtension operation=\"insert\" file=\"common-features.md\" />\nAfter" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common-features.md", FilePath = "/test/common-features.md", Content = "Inserted content" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo("Before\nInserted content\nAfter"));
    }

    [Test]
    public void BuildDocumentation_WithDuplicateInserts_ReplacesAllOccurrences()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "Start\n<MarkDownExtension operation=\"insert\" file=\"common.md\" />\nMiddle\n<MarkDownExtension operation=\"insert\" file=\"common.md\" />\nEnd" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common.md", FilePath = "/test/common.md", Content = "CONTENT" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var expected = "Start\nCONTENT\nMiddle\nCONTENT\nEnd";
        Assert.That(result[0].Content, Is.EqualTo(expected));
    }

    [Test]
    public void BuildDocumentation_WithEmptySourceContent_InsertsEmptyString()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "Before <MarkDownExtension operation=\"insert\" file=\"empty.md\" /> After" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "empty.md", FilePath = "/test/empty.md", Content = "" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Is.EqualTo("Before  After"));
    }

    [Test]
    public void BuildDocumentation_WithComplexExample_MatchesFeatureSpecification()
    {
        // This test matches the exact example from doc/features.md
        
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "windows-features.md", FilePath = "/test/windows-features.md", Content = " * windows feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.md\" />" },
            new MarkdownDocument { FileName = "ubuntu-features.md", FilePath = "/test/ubuntu-features.md", Content = " * linux feature\n <MarkDownExtension operation=\"insert\" file=\"common-features.md\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common-features.md", FilePath = "/test/common-features.md", Content = " * common feature" }
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        
        var windowsFeatures = result.First(r => r.FileName == "windows-features.md");
        Assert.That(windowsFeatures.Content, Is.EqualTo(" * windows feature\n  * common feature"));
        
        var ubuntuFeatures = result.First(r => r.FileName == "ubuntu-features.md");
        Assert.That(ubuntuFeatures.Content, Is.EqualTo(" * linux feature\n  * common feature"));
    }

    [Test]
    public void BuildDocumentation_Should_Log_Source_Documents_Without_Errors()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MarkdownCombinationService>>();
        var service = new MarkdownCombinationService(logger);

        var templateDocuments = new[]
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "# Template\n<MarkDownExtension operation=\"insert\" file=\"source1.md\" />\n<MarkDownExtension operation=\"insert\" file=\"source2.md\" />" }
        };

        var sourceDocuments = new[]
        {
            new MarkdownDocument { FileName = "source1.md", FilePath = "/test/source1.md", Content = "Source 1 content" },
            new MarkdownDocument { FileName = "source2.md", FilePath = "/test/source2.md", Content = "Source 2 content" },
            new MarkdownDocument { FileName = "source3.md", FilePath = "/test/source3.md", Content = "Source 3 content" } // This one won't be used
        };

        // Act & Assert - Should not throw any exceptions
        Assert.DoesNotThrow(() =>
        {
            var result = service.BuildDocumentation(templateDocuments, sourceDocuments);
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
        var logger = Substitute.For<ILogger<MarkdownCombinationService>>();
        var service = new MarkdownCombinationService(logger);

        var templateDocuments = new[]
        {
            new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "# Template with no inserts" }
        };

        var sourceDocuments = new MarkdownDocument[0]; // Empty array

        // Act & Assert - Should not throw any exceptions
        Assert.DoesNotThrow(() =>
        {
            var result = service.BuildDocumentation(templateDocuments, sourceDocuments);
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
        var logger = Substitute.For<ILogger<MarkdownCombinationService>>();
        var service = new MarkdownCombinationService(logger);

        var templateDocuments = new[]
        {
            new MarkdownDocument { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1" },
            new MarkdownDocument { FileName = "subfolder/template2.mdext", FilePath = "/test/subfolder/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"source.mdsrc\" />" }
        };

        var sourceDocuments = new[]
        {
            new MarkdownDocument { FileName = "source.mdsrc", FilePath = "/test/source.mdsrc", Content = "Source content" }
        };

        // Act
        var result = service.BuildDocumentation(templateDocuments, sourceDocuments);
        var resultList = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultList.Count, Is.EqualTo(2), "Should process both templates");
            
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
        var logger = Substitute.For<ILogger<MarkdownCombinationService>>();
        var service = new MarkdownCombinationService(logger);

        var templateDocuments = new[]
        {
            new MarkdownDocument { FileName = "error-template.mdext", FilePath = "/test/error-template.mdext", Content = "# Template\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />" }
        };

        var sourceDocuments = new MarkdownDocument[0]; // Empty - will cause missing source

        // Act
        var result = service.BuildDocumentation(templateDocuments, sourceDocuments);
        var resultList = result.ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultList.Count, Is.EqualTo(1), "Should still process template despite missing source");
            Assert.That(resultList[0].FileName, Is.EqualTo("error-template.md"), "Should change extension to .md even when processing encounters missing sources");
            Assert.That(resultList[0].Content, Does.Contain("<!-- Missing source: missing.mdsrc -->"), "Should contain missing source comment");
        });
    }

    #region Validation Tests

    [Test]
    public void Validate_WithNullTemplateDocument_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceDocuments = new List<MarkdownDocument>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.Validate(null!, sourceDocuments));
    }

    [Test]
    public void Validate_WithNullSourceDocuments_ThrowsArgumentNullException()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "content" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.Validate(templateDocument, null!));
    }

    [Test]
    public void Validate_WithEmptyContent_ReturnsValidResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = "" };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithNullContent_ReturnsValidResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.md", FilePath = "/test/template.md", Content = null! };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithValidInsertDirective_ReturnsValidResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"common/common.mdsrc\" />" };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common/common.mdsrc", FilePath = "/test/common/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithMissingSourceFile_ReturnsErrorResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />" };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("Source document not found: 'missing.mdsrc'"));
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
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"\" />" };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("MarkDownExtension directive is missing filename"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("<MarkDownExtension operation=\"insert\" file=\"\" />"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithInvalidFilenameCharacters_ReturnsErrorResult()
    {
        // Arrange - Use a null character which is definitely invalid
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"invalid\0file.mdsrc\" />" };
        var sourceDocuments = new List<MarkdownDocument>();

    {
        // Arrange - Use one or more invalid filename characters for the current platform
        var invalidChars = new string(Path.GetInvalidFileNameChars().Where(c => c != '\0').Take(2).ToArray());
        var invalidFileName = $"invalid{invalidChars}file.mdsrc";
        var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = $"# Title\n\n<MarkDownExtension operation=\"insert\" file=\"{invalidFileName}\" />" };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Does.StartWith("MarkDownExtension directive contains invalid filename characters:"));
            Assert.That(result.Errors[0].DirectivePath, Does.Contain("invalid"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithDuplicateDirectives_ReturnsWarningResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument
    {
        FileName = "template.mdext",
        FilePath = "/test/template.mdext",
        Content =
            "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />\n\nSome content\n\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />"
    };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common.mdsrc", FilePath = "/test/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Is.EqualTo("Duplicate MarkDownExtension directive found: '<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />'"));
            Assert.That(result.Warnings[0].DirectivePath, Is.EqualTo("common.mdsrc"));
            Assert.That(result.Warnings[0].LineNumber, Is.EqualTo(7));
        });
    }

    [Test]
    public void Validate_WithMultipleDirectivesOnSameLine_ValidatesEach()
    {
        // Arrange
    var templateDocument = new MarkdownDocument
    {
        FileName = "template.mdext",
        FilePath = "/test/template.mdext",
        Content =
            "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" /> and <MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />"
    };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "valid.mdsrc", FilePath = "/test/valid.mdsrc", Content = "Valid content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("Source document not found: 'missing.mdsrc'"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithCaseInsensitiveFilenames_ValidatesCorrectly()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"COMMON.MDSRC\" />" };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common.mdsrc", FilePath = "/test/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithDirectiveWithSpaces_ValidatesCorrectly()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"common.mdsrc\" />" };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "common.mdsrc", FilePath = "/test/common.mdsrc", Content = "Common content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithCircularReference_ReturnsWarningResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"circular1.mdsrc\" />" };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "circular1.mdsrc", FilePath = "/test/circular1.mdsrc", Content = "Content <MarkDownExtension operation=\"insert\" file=\"circular2.mdsrc\" />" },
            new MarkdownDocument { FileName = "circular2.mdsrc", FilePath = "/test/circular2.mdsrc", Content = "Content <MarkDownExtension operation=\"insert\" file=\"circular1.mdsrc\" />" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Does.StartWith("Potential circular reference detected"));
        });
    }

    [Test]
    public void Validate_WithNestedValidDirectives_ReturnsValidResult()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"level1.mdsrc\" />" };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "level1.mdsrc", FilePath = "/test/level1.mdsrc", Content = "Level 1 <MarkDownExtension operation=\"insert\" file=\"level2.mdsrc\" />" },
            new MarkdownDocument { FileName = "level2.mdsrc", FilePath = "/test/level2.mdsrc", Content = "Level 2 content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithMixedValidAndInvalidDirectives_ReturnsAppropriateResults()
    {
        // Arrange
    var templateDocument = new MarkdownDocument { FileName = "template.mdext", FilePath = "/test/template.mdext", Content = "# Title\n\n<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"\" />" };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "valid.mdsrc", FilePath = "/test/valid.mdsrc", Content = "Valid content" }
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

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
            Assert.That(result.Warnings[0].Message, Is.EqualTo("Duplicate MarkDownExtension directive found: '<MarkDownExtension operation=\"insert\" file=\"valid.mdsrc\" />'"));
        });
    }

    #endregion

    #region ValidateAll Tests

    [Test]
    public void ValidateAll_WithNullTemplateDocuments_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceDocuments = new List<MarkdownDocument>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.ValidateAll(null!, sourceDocuments));
    }

    [Test]
    public void ValidateAll_WithNullSourceDocuments_ThrowsArgumentNullException()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.ValidateAll(templateDocuments, null!));
    }

    [Test]
    public void ValidateAll_WithEmptyTemplateDocuments_ReturnsValidResult()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>();
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Content 1" }
        };

        // Act
        var result = _service.ValidateAll(templateDocuments, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void ValidateAll_WithValidTemplateDocuments_ReturnsValidResult()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />" },
            new MarkdownDocument { FileName = "template2.mdext", FilePath = "/test/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Content 1" },
            new MarkdownDocument { FileName = "source2.mdsrc", FilePath = "/test/source2.mdsrc", Content = "Content 2" }
        };

        // Act
        var result = _service.ValidateAll(templateDocuments, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void ValidateAll_WithErrorsInMultipleTemplates_CombinesAllErrors()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1\n<MarkDownExtension operation=\"insert\" file=\"missing1.mdsrc\" />" },
            new MarkdownDocument { FileName = "template2.mdext", FilePath = "/test/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"missing2.mdsrc\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "available.mdsrc", FilePath = "/test/available.mdsrc", Content = "Available content" }
        };

        // Act
        var result = _service.ValidateAll(templateDocuments, sourceDocuments);

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
    public void ValidateAll_WithMixedValidAndInvalidTemplates_ReturnsOnlyErrors()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "valid.mdext", FilePath = "/test/valid.mdext", Content = "# Valid Template\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />" },
            new MarkdownDocument { FileName = "invalid.mdext", FilePath = "/test/invalid.mdext", Content = "# Invalid Template\n<MarkDownExtension operation=\"insert\" file=\"missing.mdsrc\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Available content" }
        };

        // Act
        var result = _service.ValidateAll(templateDocuments, sourceDocuments);

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
    public void ValidateAll_WithWarningsInMultipleTemplates_CombinesAllWarnings()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "template1.mdext", FilePath = "/test/template1.mdext", Content = "# Template 1\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"source1.mdsrc\" />" },
            new MarkdownDocument { FileName = "template2.mdext", FilePath = "/test/template2.mdext", Content = "# Template 2\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />\n<MarkDownExtension operation=\"insert\" file=\"source2.mdsrc\" />" }
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new MarkdownDocument { FileName = "source1.mdsrc", FilePath = "/test/source1.mdsrc", Content = "Content 1" },
            new MarkdownDocument { FileName = "source2.mdsrc", FilePath = "/test/source2.mdsrc", Content = "Content 2" }
        };

        // Act
        var result = _service.ValidateAll(templateDocuments, sourceDocuments);

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

    #endregion
}