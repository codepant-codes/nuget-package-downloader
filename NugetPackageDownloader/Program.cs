using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NugetPackageDownloader.Config;
using NugetPackageDownloader.Enums;
using NugetPackageDownloader.GitHub;

namespace NugetPackageDownloader
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            _ = await Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsedAsync(async (result) =>
              {
                  var host = CreateHostBuilder(args, result).Build();

                  switch (result.SourceType)
                  {
                      case SourceType.GitHub:
                          IGitHubService? gitHubService = host.Services.GetService<IGitHubService>();
                          await gitHubService?.DownloadAsync()!;
                          break;
                      case SourceType.TeamCity:
                          break;
                  }
                  Console.WriteLine("Press any key to exit");
                  Console.Read();
              });
        }

        private static IHostBuilder CreateHostBuilder(string[] args, CommandLineOptions commandLineOptions)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddNugetPackageDownloaderServices(commandLineOptions);
                });

            return hostBuilder;
        }
    }
}
