namespace NugetPackageDownloader.Teamcity
{
    public interface ITeamcityService
    {
        Task DownloadAsync();
    }
}