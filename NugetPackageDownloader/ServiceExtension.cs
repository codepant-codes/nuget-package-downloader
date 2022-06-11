using Microsoft.Extensions.DependencyInjection;
using NugetPackageDownloader.Config;
using NugetPackageDownloader.Enums;
using NugetPackageDownloader.GitHub;

namespace NugetPackageDownloader
{
    internal static class ServiceExtension
    {
        public static void AddNugetPackageDownloaderServices(this IServiceCollection services, CommandLineOptions commandLineOptions)
        {
            services.AddHttpClient();


            switch (commandLineOptions.SourceType)
            {
                case SourceType.GitHub:
                    Console.WriteLine($"{SourceType.GitHub.ToString()} Source");
                    services.AddGitHubServices(commandLineOptions);
                    break;
                case SourceType.TeamCity:
                    Console.WriteLine($"{SourceType.TeamCity.ToString()} Source");
                    break;
                default:
                    Console.WriteLine($"Invalid Source");
                    throw new Exception("Invalid Source");
            }
        }

        private static void AddGitHubServices(this IServiceCollection services, CommandLineOptions commandLineOptions)
        {
            services.AddOptions<GitHubConfiguration>();
            services.PostConfigure<GitHubConfiguration>(customOptions =>
            {
                customOptions.Source = commandLineOptions.Source;
                customOptions.Destination = commandLineOptions.Destination;
                customOptions.Username = commandLineOptions.Username;
                customOptions.Password = commandLineOptions.Password;
            });

            services.AddSingleton<IGitHubService, GitHubService>();
        }
    }
}
