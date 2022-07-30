using System.ComponentModel;

namespace NugetPackageDownloader.Enums
{
    internal enum SourceType
    {
        [Description("Github")]
        Github,

        [Description("Teamcity")]
        Teamcity
    }
}
