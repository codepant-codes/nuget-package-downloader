using CommandLine;
using NugetPackageDownloader.Config;
using NugetPackageDownloader.Enums;

namespace NugetPackageDownloader
{

    class Program
    {
        public static async Task Main(string[] args)
        {
            var parsedAsync = await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(Run);
            await parsedAsync.WithNotParsedAsync(HandleParseError);
        }

        private static async Task HandleParseError(IEnumerable<Error> errs)
        {
            if (errs.IsVersion())
            {
                Console.WriteLine("Version Request");
                return;
            }

            if (errs.IsHelp())
            {
                Console.WriteLine("Help Request");
                return;
            }
            Console.WriteLine("Parser Fail");
        }

        private static async Task Run(Options opts)
        {
            switch (opts.SourceType)
            {
                case SourceType.GitHub:
                    Console.WriteLine($"{SourceType.GitHub.ToString()} Source");
                    await GitHubHandler(new GitHubConfiguration
                    {
                        Source = opts.Source,
                        Username = opts.Username,
                        Password = opts.Password,
                        Destination = opts.Destination
                    });
                    break;
                case SourceType.TeamCity:
                    Console.WriteLine($"{SourceType.TeamCity.ToString()} Source");
                    break;
                default:
                    Console.WriteLine($"Invalid Source");
                    throw new Exception("Invalid Source");
            }
        }

        private static async Task GitHubHandler(GitHubConfiguration gitHubConfiguration)
        {
            GitHub gitHub = new GitHub(gitHubConfiguration);
            await gitHub.DownloadAsync();
        }
    }
}
