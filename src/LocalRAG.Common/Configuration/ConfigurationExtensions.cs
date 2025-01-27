using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalRAG.Common.Configuration;
public static class ConfigurationExtensions
{
    public static HostApplicationBuilder AddAppSettings(this HostApplicationBuilder builder)
    {
        builder.Configuration.SetBasePath(Directory.GetCurrentDirectory());
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);                        // TODO: Move to better model than json files
        builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

        return builder;
    }
}