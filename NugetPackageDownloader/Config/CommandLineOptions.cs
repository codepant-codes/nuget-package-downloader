using CommandLine;
using NugetPackageDownloader.Enums;

namespace NugetPackageDownloader.Config
{
    internal class CommandLineOptions
    {
        [Option(shortName: 't', longName: "type", Default = SourceType.Github, Required = true, HelpText = $"Type of Source Github or Teamcity")]
        public SourceType SourceType { get; set; }

        [Option(shortName: 's', longName: "source", Default = "https://nuget.pkg.github.com/microsoft/index.json", Required = true, HelpText = "Index Source URL")]
        public string Source { get; set; }

        [Option(shortName: 'd', longName: "destination", Required = true, HelpText = "Download Directory Absolute Path")]
        public string Destination { get; set; }

        [Option(shortName: 'u', longName: "username", Required = true, HelpText = "Username")]
        public string Username { get; set; }

        [Option(shortName: 'p', longName: "password", Required = true, HelpText = "Password")]
        public string Password { get; set; }


    }
}
