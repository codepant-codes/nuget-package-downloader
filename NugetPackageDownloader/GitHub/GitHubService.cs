using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NugetPackageDownloader.Config;
using NugetPackageDownloader.Models;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;

namespace NugetPackageDownloader.GitHub
{
    public class GitHubService : IGitHubService, IDisposable
    {
        private readonly GitHubConfiguration _gitHubConfiguration;
        private readonly IHttpClientFactory _clientFactory;
        private readonly RestClient _restClient; // TODO: Remove RestSharp Usage
        private readonly AuthenticationHeaderValue _authenticationHeaderValue;
        private readonly ILogger<GitHubService> _logger;
        private const int BatchSize = 5;

        public GitHubService(IOptionsSnapshot<GitHubConfiguration> optionsSnapshot, IHttpClientFactory clientFactory, ILogger<GitHubService> logger)
        {
            this._logger = logger;
            this._gitHubConfiguration = optionsSnapshot.Value;
            this._authenticationHeaderValue = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(
                    $"{this._gitHubConfiguration.Username}:{this._gitHubConfiguration.Password}"
                )));
            this._clientFactory = clientFactory;
            this._restClient = new RestSharp.RestClient()
            {
                Authenticator = new HttpBasicAuthenticator(this._gitHubConfiguration.Username, this._gitHubConfiguration.Password)
            };
            this._restClient.UseNewtonsoftJson();
        }

        public async Task DownloadAsync()
        {


            this._logger.LogInformation("Getting Organization information");
            var queryUrl = await this.GetIndex();
            this._logger.LogInformation("Finished");



            this._logger.LogInformation("Getting List of Packages");
            var allPackages = await this.ListAllPackages(queryUrl);
            this._logger.LogInformation($"Finished");


            this._logger.LogInformation("Getting Packages Information");

            List<(string packageName, string packageVersion, string packageUrl)> packageVersionsToFetch = new List<(string packageName, string packageVersion, string packageUrl)>();
            for (int i = 0; i < allPackages.Data.Count; i++)
            {
                for (int j = 0; j < allPackages.Data[i].Versions.Count; j++)
                {
                    if (!packageVersionsToFetch.Any(x => x.packageUrl.Equals(allPackages.Data[i].Versions[j].Id)))
                    {
                        packageVersionsToFetch.Add((allPackages.Data[i].Id, allPackages.Data[i].Versions[j].Version, allPackages.Data[i].Versions[j].Id));
                    }
                }
            }

            int numberOfBatchesForPackageInfo = packageVersionsToFetch.Count / BatchSize;
            List<PackageInfoResponse> packageInfoResponses = new List<PackageInfoResponse>();
            for (int i = 0; i < numberOfBatchesForPackageInfo; i++)
            {
                List<Task<PackageInfoResponse>> packageInformationTaskBatch = new List<Task<PackageInfoResponse>>();
                this._logger.LogInformation($"Batch {i + 1} / {numberOfBatchesForPackageInfo} for Getting Packages Information");
                var batch = packageVersionsToFetch.Skip(i * BatchSize).Take(BatchSize).ToList();
                for (var index = 0; index < batch.Count; index++)
                {
                    var package = batch[index];
                    var getPackageInfoTask = this.GetPackageInfo(packageName: $"{package.packageName}:{package.packageVersion}", package.packageUrl);
                    //await getPackageInfoTask; // If this await is removed then it is not working, some requests failing
                    packageInformationTaskBatch.Add(getPackageInfoTask);
                }

                _ = Task.WhenAll(packageInformationTaskBatch).Result;
                packageInfoResponses.AddRange(packageInformationTaskBatch.Select(t => t.Result));
            }
            this._logger.LogInformation("Finished");


            List<string> nupkgUrlList = (from pkg in packageInfoResponses orderby pkg.PackageContent select pkg.PackageContent).Distinct().ToList();
            var numberOfBatchesForDownload = nupkgUrlList.Count / BatchSize;
            this._logger.LogInformation($"Downloading {nupkgUrlList.Count} packages in {numberOfBatchesForDownload} batches");
            for (int index = 0; index < numberOfBatchesForDownload; index++)
            {
                List<Task> downloadTasksList = new List<Task>();
                this._logger.LogInformation($"Batch {index + 1} / {numberOfBatchesForDownload} for Downloading Packages");
                var batch = nupkgUrlList.Skip(index * BatchSize).Take(BatchSize).ToList();
                for (var j = 0; j < batch.Count; j++)
                {
                    var t = this.DownloadFile(batch[j], this._gitHubConfiguration.Destination);
                    downloadTasksList.Add(t);
                }
                await Task.WhenAll(downloadTasksList);
            }
            this._logger.LogInformation("Finished Downloading");

            this._logger.LogInformation("Press Any Key to Exit");
            Console.ReadLine();
        }

        private async Task<string> GetIndex()
        {
            this._logger.LogInformation($"Getting GitHub Source");
            var response = await this._restClient.GetAsync<BaseIndexResponse>(new RestRequest(this._gitHubConfiguration.Source));
            this._logger.LogInformation($"Finished");
            return response?.Resources.FirstOrDefault(x => x.Type.Equals("SearchQueryService"))?.Id ?? throw new Exception($"Something went wrong in getting {nameof(this.GetIndex)}");
        }

        private async Task<ListAllPackagesResponse> ListAllPackages(string queryUrl)
        {
            this._logger.LogInformation($"Querying Packages");
            queryUrl += "?ignoreFilter=true&prerelease=true";
            var response = await this._restClient.GetAsync<ListAllPackagesResponse>(new RestRequest(queryUrl));
            this._logger.LogInformation($"Finished");
            return response ?? throw new Exception($"Something went wrong in getting {nameof(this.ListAllPackages)}");
        }

        private async Task<PackageInfoResponse> GetPackageInfo(string packageName, string url)
        {
            this._logger.LogInformation($"{ packageName} : Querying Package Info");
            var httpClient = this._clientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = this._authenticationHeaderValue;
            var response = await httpClient.GetAsync(url);
            this._logger.LogInformation($"{packageName} : Validating Response");
            if (response.IsSuccessStatusCode)
            {
                var t = JsonConvert.DeserializeObject<PackageInfoResponse>(await response.Content.ReadAsStringAsync());
                this._logger.LogInformation($"{packageName} : Finished");
                return t ?? throw new Exception($"Something went wrong in getting {nameof(this.GetPackageInfo)}");
            }
            else
            {
                throw new Exception($"Something went wrong in getting {nameof(this.GetPackageInfo)}");
            }
        }

        private async Task DownloadFile(string url, string destinationDirectoryPath)
        {
            try
            {
                this._logger.LogInformation($"Downloading");
                var splits = url.Split('/');
                this._logger.LogInformation($"Getting File Name");
                var fileName = splits[splits.Length - 1];
                this._logger.LogInformation($"{fileName}");
                var destinationPath = destinationDirectoryPath + Path.DirectorySeparatorChar + fileName;
                if (File.Exists(destinationPath))
                {
                    this._logger.LogInformation($"Deleting {fileName}");
                    File.Delete(destinationPath);
                }
                this._logger.LogInformation($"Downloading {fileName}");
                using (var httpClient = this._clientFactory.CreateClient(nameof(GitHubService)))
                {
                    httpClient.DefaultRequestHeaders.Authorization = _authenticationHeaderValue;
                    this._logger.LogInformation($"Downloading Started {fileName}");
                    var response = await httpClient.GetAsync(url);
                    this._logger.LogInformation($"Downloading Completed{fileName}");
                    using (var fs = new FileStream(destinationPath, FileMode.CreateNew))
                    {
                        this._logger.LogInformation($"Saving {fileName}");
                        await response.Content.CopyToAsync(fs);
                        this._logger.LogInformation($"Saving Completed {fileName}");
                    }
                }
            }
            catch (Exception e)
            {
                this._logger.LogError($"Error in {nameof(this.DownloadFile)} Failed for {url}", e);
            }
        }

        public void Dispose()
        {
            this._restClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
