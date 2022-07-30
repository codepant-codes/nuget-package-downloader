using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NugetPackageDownloader.Config;
using NugetPackageDownloader.Enums;
using NugetPackageDownloader.GitHub;
using NugetPackageDownloader.Teamcity;

namespace NugetPackageDownloader
{
    internal static class ServiceExtension
    {
        public static void AddNugetPackageDownloaderServices(this IServiceCollection services, CommandLineOptions commandLineOptions)
        {
            services.AddHttpClient();


            switch (commandLineOptions.SourceType)
            {
                case SourceType.Github:
                    Console.WriteLine($"{SourceType.Github.ToString()} Source");
                    services.AddGitHubServices(commandLineOptions);
                    break;
                case SourceType.Teamcity:
                    Console.WriteLine($"{SourceType.Teamcity.ToString()} Source");
                    services.AddTeamCityServices(commandLineOptions);
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

            services.TryAddTransient<IGitHubService, GitHubService>();
        }

        private static void AddTeamCityServices(this IServiceCollection services, CommandLineOptions commandLineOptions)
        {
            services.AddOptions<TeamcityConfiguration>();
            services.PostConfigure<TeamcityConfiguration>(customOptions =>
            {
                customOptions.Source = commandLineOptions.Source;
                customOptions.Destination = commandLineOptions.Destination;
                customOptions.Username = commandLineOptions.Username;
                customOptions.Password = commandLineOptions.Password;
            });

            services.TryAddTransient<ITeamcityService, TeamcityService>();
        }
    }
}
