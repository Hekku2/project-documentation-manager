using Desktop.Configuration;
using Desktop.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desktop.Factories;

public class SettingsContentViewModelFactory(ILoggerFactory loggerFactory, IOptions<ApplicationOptions> applicationOptions) : ISettingsContentViewModelFactory
{
    public SettingsContentViewModel Create()
    {
        return new SettingsContentViewModel(loggerFactory.CreateLogger<SettingsContentViewModel>())
        {
            ApplicationOptions = applicationOptions.Value
        };
    }
}
