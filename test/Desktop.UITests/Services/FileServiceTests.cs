using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Desktop.Services;
using Desktop.Configuration;

namespace Desktop.UITests.Services;

[TestFixture]
public class FileServiceTests
{
    private FileService _fileService;
    private ApplicationOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new ApplicationOptions
        {
            DefaultProjectFolder = Path.Combine(Path.GetTempPath(), "FileServiceTests", Guid.NewGuid().ToString())
        };

        Directory.CreateDirectory(_options.DefaultProjectFolder);

        var optionsWrapper = Options.Create(_options);
        var logger = NullLoggerFactory.Instance.CreateLogger<FileService>();
        _fileService = new FileService(logger, optionsWrapper);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_options.DefaultProjectFolder))
        {
            try
            {
                Directory.Delete(_options.DefaultProjectFolder, true);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"TearDown cleanup failed: {ex}");
            }
        }
    }

}