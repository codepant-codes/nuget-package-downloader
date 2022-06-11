using System.ComponentModel;

namespace NugetPackageDownloader.Enums
{
    internal enum SourceType
    {
        [Description("GitHub")]
        GitHub,

        [Description("TeamCity")]
        TeamCity
    }
}
