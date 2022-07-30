using Newtonsoft.Json;

namespace NugetPackageDownloader.GitHub.Models
{
    internal class PackageInfoResponse
    {

        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("packageContent")]
        public string PackageContent { get; set; }

        [JsonProperty("catalogEntry")]
        public CatalogEntry CatalogEntry { get; set; }
    }
    internal class CatalogEntry
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("authors")]
        public string Authors { get; set; }

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("dependencyGroups")]
        public List<DependencyGroup> DependencyGroups { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty("id")]
        public string IdName { get; set; }

        [JsonProperty("isPrerelease")]
        public bool IsPrerelease { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("licenseUrl")]
        public string LicenseUrl { get; set; }

        [JsonProperty("packageContent")]
        public string PackageContent { get; set; }

        [JsonProperty("projectUrl")]
        public string ProjectUrl { get; set; }

        [JsonProperty("requireLicenseAcceptance")]
        public bool RequireLicenseAcceptance { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("tags")]
        public string Tags { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    internal class DependencyGroup
    {
        [JsonProperty("@id")]
        public string Id { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonProperty("dependencies")]
        public List<object> Dependencies { get; set; }

        [JsonProperty("targetFramework")]
        public string TargetFramework { get; set; }
    }
}
