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
            new("template.md", "# Title\n\nThis is regular content without inserts.")
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
            new("windows-features.md", " * windows feature\n <insert common-features.md>")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common-features.md", " * common feature")
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
            new("combined.md", "# Features\n\n<insert intro.md>\n\n<insert details.md>\n\n<insert conclusion.md>")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("intro.md", "This is the introduction."),
            new("details.md", "Here are the details."),
            new("conclusion.md", "This is the conclusion.")
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var processedContent = result[0].Content;
        Assert.That(processedContent, Does.Contain("This is the introduction."));
        Assert.That(processedContent, Does.Contain("Here are the details."));
        Assert.That(processedContent, Does.Contain("This is the conclusion."));
        Assert.That(processedContent, Does.Not.Contain("<insert"));
    }

    [Test]
    public void BuildDocumentation_WithMultipleTemplates_ProcessesAllTemplates()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new("windows-features.md", " * windows feature\n <insert common-features.md>"),
            new("ubuntu-features.md", " * linux feature\n <insert common-features.md>")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common-features.md", " * common feature")
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
            new("template.md", "# Title\n\n<insert missing-file.md>\n\nEnd.")
        };
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Content, Does.Contain("<!-- Missing source: missing-file.md -->"));
        Assert.That(result[0].Content, Does.Not.Contain("<insert missing-file.md>"));
    }

    [Test]
    public void BuildDocumentation_WithNestedInserts_ProcessesRecursively()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new("main.md", "# Main\n\n<insert level1.md>")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("level1.md", "## Level 1\n\n<insert level2.md>"),
            new("level2.md", "### Level 2 Content")
        };

        // Act
        var result = _service.BuildDocumentation(templateDocuments, sourceDocuments).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var processedContent = result[0].Content;
        Assert.That(processedContent, Does.Contain("# Main"));
        Assert.That(processedContent, Does.Contain("## Level 1"));
        Assert.That(processedContent, Does.Contain("### Level 2 Content"));
        Assert.That(processedContent, Does.Not.Contain("<insert"));
    }

    [Test]
    public void BuildDocumentation_WithCaseInsensitiveFilenames_MatchesCorrectly()
    {
        // Arrange
        var templateDocuments = new List<MarkdownDocument>
        {
            new("template.md", "<insert COMMON-FEATURES.MD>")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common-features.md", "Common content")
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
            new("template.md", "Before\n<insert   common-features.md   >\nAfter")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common-features.md", "Inserted content")
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
            new("template.md", "Start\n<insert common.md>\nMiddle\n<insert common.md>\nEnd")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common.md", "CONTENT")
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
            new("template.md", "Before <insert empty.md> After")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("empty.md", "")
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
            new("windows-features.md", " * windows feature\n <insert common-features.md>"),
            new("ubuntu-features.md", " * linux feature\n <insert common-features.md>")
        };
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common-features.md", " * common feature")
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
            new MarkdownDocument("template.md", "# Template\n<insert source1.md>\n<insert source2.md>")
        };

        var sourceDocuments = new[]
        {
            new MarkdownDocument("source1.md", "Source 1 content"),
            new MarkdownDocument("source2.md", "Source 2 content"),
            new MarkdownDocument("source3.md", "Source 3 content") // This one won't be used
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
            new MarkdownDocument("template.md", "# Template with no inserts")
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
            new MarkdownDocument("template1.mdext", "# Template 1"),
            new MarkdownDocument("subfolder/template2.mdext", "# Template 2\n<insert source.mdsrc>")
        };

        var sourceDocuments = new[]
        {
            new MarkdownDocument("source.mdsrc", "Source content")
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
            new MarkdownDocument("error-template.mdext", "# Template\n<insert missing.mdsrc>")
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
        var templateDocument = new MarkdownDocument("template.md", "content");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.Validate(templateDocument, null!));
    }

    [Test]
    public void Validate_WithEmptyContent_ReturnsValidResult()
    {
        // Arrange
        var templateDocument = new MarkdownDocument("template.md", "");
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
        var templateDocument = new MarkdownDocument("template.md", null!);
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
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert common/common.mdsrc>");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common/common.mdsrc", "Common content")
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
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert missing.mdsrc>");
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
            Assert.That(result.Errors[0].SourceContext, Is.EqualTo("<insert missing.mdsrc>"));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithEmptyFilename_ReturnsErrorResult()
    {
        // Arrange
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert >");
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Is.EqualTo("Insert directive is missing filename"));
            Assert.That(result.Errors[0].DirectivePath, Is.EqualTo("<insert >"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithInvalidFilenameCharacters_ReturnsErrorResult()
    {
        // Arrange - Use a null character which is definitely invalid
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert invalid\0file.mdsrc>");
        var sourceDocuments = new List<MarkdownDocument>();

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0].Message, Does.StartWith("Insert directive contains invalid filename characters:"));
            Assert.That(result.Errors[0].DirectivePath, Does.Contain("invalid"));
            Assert.That(result.Errors[0].LineNumber, Is.EqualTo(3));
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    [Test]
    public void Validate_WithDuplicateDirectives_ReturnsWarningResult()
    {
        // Arrange
        var templateDocument = new MarkdownDocument("template.mdext", 
            "# Title\n\n<insert common.mdsrc>\n\nSome content\n\n<insert common.mdsrc>");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common.mdsrc", "Common content")
        };

        // Act
        var result = _service.Validate(templateDocument, sourceDocuments);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Is.EqualTo("Duplicate insert directive found: '<insert common.mdsrc>'"));
            Assert.That(result.Warnings[0].DirectivePath, Is.EqualTo("common.mdsrc"));
            Assert.That(result.Warnings[0].LineNumber, Is.EqualTo(7));
        });
    }

    [Test]
    public void Validate_WithMultipleDirectivesOnSameLine_ValidatesEach()
    {
        // Arrange
        var templateDocument = new MarkdownDocument("template.mdext", 
            "# Title\n\n<insert valid.mdsrc> and <insert missing.mdsrc>");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("valid.mdsrc", "Valid content")
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
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert COMMON.MDSRC>");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common.mdsrc", "Common content")
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
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert   common.mdsrc   >");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("common.mdsrc", "Common content")
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
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert circular1.mdsrc>");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("circular1.mdsrc", "Content <insert circular2.mdsrc>"),
            new("circular2.mdsrc", "Content <insert circular1.mdsrc>")
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
        var templateDocument = new MarkdownDocument("template.mdext", "# Title\n\n<insert level1.mdsrc>");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("level1.mdsrc", "Level 1 <insert level2.mdsrc>"),
            new("level2.mdsrc", "Level 2 content")
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
        var templateDocument = new MarkdownDocument("template.mdext", 
            "# Title\n\n<insert valid.mdsrc>\n<insert missing.mdsrc>\n<insert valid.mdsrc>\n<insert >");
        var sourceDocuments = new List<MarkdownDocument>
        {
            new("valid.mdsrc", "Valid content")
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
            Assert.That(result.Errors.Any(e => e.Message.Contains("Insert directive is missing filename")), Is.True);
            
            // Check for duplicate warning
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0].Message, Is.EqualTo("Duplicate insert directive found: '<insert valid.mdsrc>'"));
        });
    }

    #endregion
}