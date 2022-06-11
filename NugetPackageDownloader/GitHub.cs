using System.Collections.Concurrent;
using System.Net;
using Konsole;
using Newtonsoft.Json;
using NugetPackageDownloader.Config;
using NugetPackageDownloader.Models;
using Polly;
using Polly.Retry;

namespace NugetPackageDownloader
{
    public class GitHub : IDisposable
    {
        private readonly GitHubConfiguration _gitHubConfiguration;
        private readonly HttpClient _httpClient;

        private readonly AsyncRetryPolicy policy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
            5,
            attempt => TimeSpan.FromMilliseconds(200),
            (exception, calculatedWaitDuration) => // Capture some info for logging!
            {
                Console.WriteLine("Retrying");
            });

        public GitHub(GitHubConfiguration gitHubConfiguration)
        {

            this._gitHubConfiguration = gitHubConfiguration;
            this._httpClient = new HttpClient(new HttpClientHandler()
            {
                Credentials = new NetworkCredential(this._gitHubConfiguration.Username, this._gitHubConfiguration.Password)
            });
        }

        public async Task DownloadAsync()
        {
            var bars = new ConcurrentBag<ProgressBar>();

            Console.WriteLine("Getting Organization information");
            var getIndexProgressBar = new ProgressBar(100);
            bars.Add(getIndexProgressBar);
            var queryUrl = await this.GetIndex(getIndexProgressBar);
            getIndexProgressBar.Refresh(100, "Finished");



            Console.WriteLine("Getting List of Packages");
            var listAllPackagesProgressBar = new ProgressBar(100);
            bars.Add(listAllPackagesProgressBar);
            var allPackages = await this.ListAllPackages(listAllPackagesProgressBar, queryUrl);
            listAllPackagesProgressBar.Refresh(100, "Finished");


            Console.WriteLine("Getting Packages Information");

            List<(string packageName, string packageVersion, string packageUrl)> packageVersionsToFetch = new List<(string packageName, string packageVersion, string packageUrl)>();
            for (int i = 0; i < allPackages.Data.Count; i++)
            {
                for (int j = 0; j < allPackages.Data[i].Versions.Count; j++)
                {
                    var isExists = packageVersionsToFetch.Where(x => x.packageUrl.Equals(allPackages.Data[i].Versions[j].Id)).Any();
                    if (!isExists)
                    {
                        packageVersionsToFetch.Add((allPackages.Data[i].Id, allPackages.Data[i].Versions[j].Version, allPackages.Data[i].Versions[j].Id));
                    }
                }
            }


            var batchSize = 2;
            int numberOfBatches = packageVersionsToFetch.Count / batchSize;
            List<PackageInfoResponse> packageInfoResponses = new List<PackageInfoResponse>();
            for (int i = 0; i < numberOfBatches; i++)
            {
                List<Task<PackageInfoResponse>> packageInformationTaskBatch = new List<Task<PackageInfoResponse>>();
                Console.WriteLine($"Batch {i + 1} for Getting Packages Information");
                var batch = packageVersionsToFetch.Skip(i * batchSize).Take(batchSize);
                foreach (var package in batch)
                {
                    var packageInfoProgressBar = new ProgressBar(100, textWidth: (package.packageName.Length + package.packageVersion.Length + 50));
                    bars.Add(packageInfoProgressBar);
                    Task<PackageInfoResponse> getPackageInfoTask = this.GetPackageInfo(packageInfoProgressBar, packageName: $"{package.packageName}:{package.packageVersion}", package.packageUrl);
                    packageInformationTaskBatch.Add(getPackageInfoTask);
                    //await getPackageInfoTask;
                }
                packageInfoResponses.AddRange(await Task.WhenAll(packageInformationTaskBatch));
            }
            Console.WriteLine("Finished");


            List<string> nupkgUrlList = (from pkg in packageInfoResponses select pkg.PackageContent).Distinct().ToList();

            Console.WriteLine("Downloading Started");
            var downloadingProgressBar = new ProgressBar(nupkgUrlList.Count, 150);
            bars.Add(downloadingProgressBar);
            for (int i = 0; i < nupkgUrlList.Count; i++)
            {
                await this.DownloadFile(nupkgUrlList[i], this._gitHubConfiguration.Destination);
                downloadingProgressBar.Refresh(i, nupkgUrlList[i]);
            }

            Console.WriteLine("Finished Downloading");
            Console.WriteLine("Press Any Key to Exit");
            Console.ReadLine();
        }

        private async Task<string> GetIndex(ProgressBar progressBar)
        {
            progressBar.Refresh(0, "Getting GitHub Source");
            var response = await this._httpClient.GetAsync(this._gitHubConfiguration.Source);
            progressBar.Refresh(50, "Checking Response");
            if (response.IsSuccessStatusCode)
            {
                progressBar.Refresh(70, "Received Source");
                BaseIndexResponse? baseIndexResponse = JsonConvert.DeserializeObject<BaseIndexResponse>(await response.Content.ReadAsStringAsync());

                progressBar.Refresh(80, "Validating Response");
                var resource = baseIndexResponse?.Resources.FirstOrDefault(x => x.Type.Equals("SearchQueryService"));
                return resource?.Id ?? throw new InvalidOperationException();
            }

            throw new Exception($"Something went wrong in getting {nameof(this.GetIndex)}");
        }

        private async Task<ListAllPackagesResponse> ListAllPackages(ProgressBar progressBar, string queryUrl)
        {
            progressBar.Refresh(0, "Querying Packages");
            var response = await this._httpClient.GetAsync(queryUrl);
            progressBar.Refresh(50, "Checking Response");
            if (response.IsSuccessStatusCode)
            {
                progressBar.Refresh(70, "Received Packages");
                ListAllPackagesResponse? listAllPackagesResponse = JsonConvert.DeserializeObject<ListAllPackagesResponse>(await response.Content.ReadAsStringAsync());

                return listAllPackagesResponse ?? throw new InvalidOperationException();
            }

            throw new Exception($"Something went wrong in getting {nameof(this.ListAllPackages)}");
        }

        private async Task<PackageInfoResponse> GetPackageInfo(ProgressBar progressBar, string packageName, string url)
        {
            return await policy.ExecuteAsync(async () =>
            {
                progressBar.Refresh(0, $"{packageName} : Querying Package Info");
                var response = await this._httpClient.GetAsync(url);
                progressBar.Refresh(50, $"{packageName} : Checking Response");
                if (response.IsSuccessStatusCode)
                {
                    progressBar.Refresh(70, $"{packageName} : Received Package Info");
                    PackageInfoResponse? packageInfoResponse = JsonConvert.DeserializeObject<PackageInfoResponse>(await response.Content.ReadAsStringAsync());

                    if (packageInfoResponse != null)
                    {
                        progressBar.Refresh(100, $"{packageName} : Finished");
                        return packageInfoResponse;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                throw new Exception($"Something went wrong in getting {nameof(this.GetPackageInfo)}");
            });
        }

        private async Task DownloadFile(string url, string destinationDirectoryPath)
        {
            var splits = url.Split('/');
            var fileName = splits[splits.Length - 1];
            var destinationPath = destinationDirectoryPath + Path.DirectorySeparatorChar + fileName;
            var response = await this._httpClient.GetAsync(url);
            using (var fs = new FileStream(destinationPath, FileMode.CreateNew))
            {
                await response.Content.CopyToAsync(fs);
            }
        }

        public void Dispose()
        {
            this._httpClient?.Dispose();
        }
    }
}
