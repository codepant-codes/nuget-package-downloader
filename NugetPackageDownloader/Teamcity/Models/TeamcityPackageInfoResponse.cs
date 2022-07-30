using Newtonsoft.Json;

namespace NugetPackageDownloader.Teamcity.Models
{
    internal class TeamcityPackageInfoResponse
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("items")]
        public List<Items> items { get; set; }
    }

    internal class Items
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("items")]
        public List<ChildItems> items { get; set; }
    }

    internal class ChildItems
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("packageContent")]
        public string packageContent { get; set; }
    }
}
