using System;
using LogGrokCore.Colors.Configuration;
using LogGrokCore.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace LogGrokCore
{
    public class ApplicationSettings
    {
        public ColorSettings ColorSettings { get; private set; } = new();

        public LogFormat[] LogFormats { get; set; } =
            Array.Empty<LogFormat>();

        public static ApplicationSettings Load()
        {
            var builder = new ConfigurationBuilder()
                .AddYamlFile("appsettings.yaml", true, true);

            var settings = new ApplicationSettings();
            
            IConfigurationRoot configuration = builder.Build();
            configuration.GetSection("Settings").Bind(settings);
            
            ChangeToken.OnChange(() => configuration.GetReloadToken(), () =>
            {
                var newSettings = new ApplicationSettings();
                configuration.GetSection("Settings").Bind(newSettings);
                settings.ColorSettings = newSettings.ColorSettings;
                settings.LogFormats = newSettings.LogFormats;
            });
            
            return settings;
        }

        private ApplicationSettings()
        {
        }
    }
}