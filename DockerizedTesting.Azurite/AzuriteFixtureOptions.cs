using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.Azurite
{
    public class AzuriteFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("mcr.microsoft.com/azure-storage/azurite:latest");

        public bool Loose { get; set; }

        public string DebugLog { get; set; }

        public string Workspace { get; set; }
        public string StorageAccountName { get; set; } = "devstoreaccount1";

        public string StorageAccountKey { get; set; } =
            "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
    }
}