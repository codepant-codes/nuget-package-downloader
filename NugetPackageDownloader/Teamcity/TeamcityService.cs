using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NugetPackageDownloader.Teamcity.Models;

namespace NugetPackageDownloader.Teamcity
{
    public class TeamcityService : ITeamcityService, IDisposable
    {
        private readonly TeamcityConfiguration _teamcityConfiguration;
        private readonly IHttpClientFactory _clientFactory;
        private readonly AuthenticationHeaderValue _authenticationHeaderValue;
        private readonly ILogger<TeamcityService> _logger;
        private readonly int ProcessorCount = Environment.ProcessorCount;

        public TeamcityService(IOptions<TeamcityConfiguration> options, IHttpClientFactory clientFactory, ILogger<TeamcityService> logger)
        {
            this._logger = logger;
            this._teamcityConfiguration = options.Value;
            this._authenticationHeaderValue = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(
                    $"{this._teamcityConfiguration.Username}:{this._teamcityConfiguration.Password}"
                )));
            this._clientFactory = clientFactory;
        }

        public async Task DownloadAsync()
        {

            this._logger.LogInformation("Getting Organization information");
            var queryUrl = await this.GetIndex();
            this._logger.LogInformation("Finished");

            this._logger.LogInformation("Getting List of Packages");
            var allPackages = await this.ListAllPackages(queryUrl);
            this._logger.LogInformation($"Finished");

            #region Extracting Package Versions to Fetch
            this._logger.LogInformation("Extracting Package Versions to Fetch");
            List<(string packageName, string packageVersion, string packageUrl)> packageVersionsToFetch = new List<(string packageName, string packageVersion, string packageUrl)>();
            for (int i = 0; i < allPackages.Data.Count; i++)
            {
                for (int j = 0; j < allPackages.Data[i].Versions.Count; j++)
                {
                    if (!packageVersionsToFetch.Any(x => x.packageUrl.Equals(allPackages.Data[i].Versions[j].Id)))
                    {
                        string fileName = $"{allPackages.Data[i].Id}.{allPackages.Data[i].Version}.nupkg";
                        var destinationFilePath = this.GetDestinationFilePath(fileName);
                        if (File.Exists(destinationFilePath))
                        {
                            this._logger.LogInformation($"Skipping {fileName} as it already exists at {destinationFilePath}");
                        }
                        else
                        {
                            packageVersionsToFetch.Add((allPackages.Data[i].Id, allPackages.Data[i].Versions[j].Version, allPackages.Data[i].Versions[j].Id));
                        }
                    }
                }
            }
            #endregion

            #region Getting Packages Information
            this._logger.LogInformation("Getting Packages Information");
            ConcurrentBag<TeamcityPackageInfoResponse> packageInfoResponsesBag = new ConcurrentBag<TeamcityPackageInfoResponse>();
            Parallel.ForEach(packageVersionsToFetch, new ParallelOptions { MaxDegreeOfParallelism = ProcessorCount }, package =>
            {
                try
                {
                    var getPackageInfo = this.GetPackageInfo(packageName: $"{package.packageName}:{package.packageVersion}", package.packageUrl).Result;
                    packageInfoResponsesBag.Add(getPackageInfo);
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, nameof(this.GetPackageInfo));
                }
            });
            List<TeamcityPackageInfoResponse> packageInfoResponses = packageInfoResponsesBag.ToList();
            #endregion

            #region Extracting nupkg Urls
            List<string> nupkgUrlList = new List<string>();
            for (int i = 0; i < packageInfoResponses.Count; i++)
            {
                for (int j = 0; j < packageInfoResponses[i].items.Count; j++)
                {
                    for (int k = 0; k < packageInfoResponses[i].items[j].items.Count; k++)
                    {
                        if (!nupkgUrlList.Any(x => x.Equals(packageInfoResponses[i].items[j].items[k].packageContent)))
                        {
                            nupkgUrlList.Add(packageInfoResponses[i].items[j].items[k].packageContent);
                        }
                    }
                }
            }
            #endregion

            #region Downloading
            this._logger.LogInformation("Started Downloading");
            Parallel.ForEach(nupkgUrlList, new ParallelOptions { MaxDegreeOfParallelism = ProcessorCount }, url =>
            {
                try
                {
                    this.DownloadFileAsync(url).Wait();
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, nameof(this.DownloadFileAsync));
                }
            });
            this._logger.LogInformation("Finished Downloading");
            #endregion
        }

        private async Task<string> GetIndex()
        {
            BaseIndexResponse? response = null;
            this._logger.LogInformation($"Getting Teamcity Source");
            using (var httpClient = this._clientFactory.CreateClient(nameof(TeamcityService)))
            {
                httpClient.DefaultRequestHeaders.Authorization = this._authenticationHeaderValue;
                var httpResponse = (await httpClient.GetAsync(this._teamcityConfiguration.Source));
                if (httpResponse.IsSuccessStatusCode)
                {
                    response = JsonConvert.DeserializeObject<BaseIndexResponse>((await httpResponse.Content.ReadAsStringAsync()) ?? string.Empty);
                }
                else
                {
                    throw new Exception($"Invalid Username: {this._teamcityConfiguration.Username} and password: {this._teamcityConfiguration.Password}");
                }
            }
            this._logger.LogInformation($"Finished");
            return response?.Resources.FirstOrDefault(x => x.Type.Equals("SearchQueryService"))?.Id ?? throw new Exception($"Something went wrong in getting {nameof(this.GetIndex)}");
        }

        private async Task<ListAllPackagesResponse> ListAllPackages(string queryUrl)
        {
            ListAllPackagesResponse? response = null;
            this._logger.LogInformation($"Querying Packages");
            queryUrl += "?ignoreFilter=true&prerelease=true";
            using (var httpClient = this._clientFactory.CreateClient(nameof(TeamcityService)))
            {
                httpClient.DefaultRequestHeaders.Authorization = this._authenticationHeaderValue;
                var httpResponse = (await httpClient.GetAsync(queryUrl));
                if (httpResponse.IsSuccessStatusCode)
                    response = JsonConvert.DeserializeObject<ListAllPackagesResponse>((await httpResponse.Content.ReadAsStringAsync()) ?? string.Empty);
            }
            this._logger.LogInformation($"Finished");
            return response ?? throw new Exception($"Something went wrong in getting {nameof(this.ListAllPackages)}");
        }

        private async Task<TeamcityPackageInfoResponse> GetPackageInfo(string packageName, string url)
        {
            try
            {
                this._logger.LogInformation($"{packageName} : Querying Package Info");
                var httpClient = this._clientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = this._authenticationHeaderValue;
                var response = await httpClient.GetAsync(url);
                this._logger.LogInformation($"{packageName} : Validating Response");
                if (response.IsSuccessStatusCode)
                {
                    var t = JsonConvert.DeserializeObject<TeamcityPackageInfoResponse>(await response.Content.ReadAsStringAsync());
                    this._logger.LogInformation($"{packageName} : Finished");
                    return t ?? throw new Exception($"Something went wrong in getting {nameof(this.GetPackageInfo)}");
                }
                else
                {
                    throw new Exception($"Something went wrong in getting {nameof(this.GetPackageInfo)}");
                }
            }
            catch (Exception e)
            {
                this._logger.LogError(e, nameof(this.GetPackageInfo));
                throw;
            }
        }

        private async Task DownloadFileAsync(string url)
        {
            try
            {
                this._logger.LogInformation($"Downloading");
                var splits = url.Split('/');
                this._logger.LogInformation($"Getting File Name");
                var fileName = splits[splits.Length - 1];
                this._logger.LogInformation($"{fileName}");
                var destinationPath = GetDestinationFilePath(fileName);
                if (File.Exists(destinationPath))
                {
                    this._logger.LogInformation($"Deleting {fileName}");
                    File.Delete(destinationPath);
                }
                this._logger.LogInformation($"Downloading {fileName}");
                using (var httpClient = this._clientFactory.CreateClient(nameof(TeamcityService)))
                {
                    httpClient.DefaultRequestHeaders.Authorization = this._authenticationHeaderValue;
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
                this._logger.LogError($"Error in {nameof(this.DownloadFileAsync)} Failed for {url}", e);
            }
        }

        private string GetDestinationFilePath(string fileName)
        {
            return this._teamcityConfiguration.Destination + Path.DirectorySeparatorChar + fileName;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
