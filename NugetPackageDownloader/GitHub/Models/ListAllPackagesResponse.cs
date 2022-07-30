using Newtonsoft.Json;

namespace NugetPackageDownloader.GitHub.Models
{
    internal class ListAllPackagesResponse
    {
        [JsonProperty("data")]
        public List<Datum> Data { get; set; }

        [JsonProperty("totalHits")]
        public int TotalHits { get; set; }
    }
    internal class Datum
    {
        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("authors")]
        public string Authors { get; set; }

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("dependencies")]
        public string Dependencies { get; set; }

        [JsonProperty("dependencyGroups")]
        public object DependencyGroups { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("isPrerelease")]
        public bool IsPrerelease { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("licenseUrl")]
        public string LicenseUrl { get; set; }

        [JsonProperty("projectUrl")]
        public string ProjectUrl { get; set; }

        [JsonProperty("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("totalDownloads")]
        public int TotalDownloads { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versions")]
        public List<VersionModel> Versions { get; set; }
    }

    internal class VersionModel
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("downloads")]
        public int Downloads { get; set; }

        [JsonProperty("@id")]
        public string Id { get; set; }
    }


}
