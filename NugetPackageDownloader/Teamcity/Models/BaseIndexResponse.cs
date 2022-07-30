using Newtonsoft.Json;

namespace NugetPackageDownloader.Teamcity.Models
{
    internal class BaseIndexResponse
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("resources")]
        public List<Resource> Resources { get; set; }
    }

    internal class Resource
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }

}
