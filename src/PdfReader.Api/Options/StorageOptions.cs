namespace PdfReader.Api.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Base path for storing PDFs and JSON results. Should be a shared volume in Docker.
    /// </summary>
    public string BasePath { get; set; } = "/data/storage";
}
