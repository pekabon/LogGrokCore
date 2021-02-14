using LogGrokCore.Colors.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace LogGrokCore
{
    public class ApplicationSettings
    {
        public ColorSettings ColorSettings { get; private set; }

        public ApplicationSettings()
        {
            ColorSettings = new ColorSettings();
            
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true);
            IConfigurationRoot configuration = builder.Build();
            configuration.GetSection("ColorSettings").Bind(ColorSettings);

            ChangeToken.OnChange(() => configuration.GetReloadToken(), () =>
            {
                var newColorSettings = new ColorSettings();
                configuration.GetSection("ColorSettings").Bind(newColorSettings);
                ColorSettings = newColorSettings;
            });
        }
    }
}