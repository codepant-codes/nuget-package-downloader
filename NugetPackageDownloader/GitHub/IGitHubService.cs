namespace NugetPackageDownloader.GitHub
{
    public interface IGitHubService
    {
        Task DownloadAsync();
    }
}