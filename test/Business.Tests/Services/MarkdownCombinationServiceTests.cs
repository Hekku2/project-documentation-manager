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
}