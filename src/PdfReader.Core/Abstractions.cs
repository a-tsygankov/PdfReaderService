using System.Text.Json;
using System.Text.Json.Serialization;

namespace PdfReader.Core;

public enum DocumentStatus
{
    Uploaded = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed class Document
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }

    public string StoragePath { get; set; } = string.Empty;
    public string? ResultPath { get; set; }

    public string? FormType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed record DocumentQueuedMessage(Guid DocumentId, int Attempt = 1);

public interface IDocumentStorage
{
    Task SavePdfAsync(Guid documentId, Stream pdfStream, CancellationToken ct = default);
    Task<Stream> GetPdfAsync(Guid documentId, CancellationToken ct = default);

    Task SaveResultJsonAsync(Guid documentId, string json, CancellationToken ct = default);
    Task<string?> GetResultJsonAsync(Guid documentId, CancellationToken ct = default);
}

public interface IQueueClient
{
    Task EnqueueAsync(DocumentQueuedMessage message, CancellationToken ct = default);
    Task<DocumentQueuedMessage?> DequeueAsync(CancellationToken ct = default);
}

public interface IPdfProcessingPipeline
{
    Task<PdfProcessingResult> ProcessAsync(
        Stream pdf,
        string? formType,
        CancellationToken ct = default);
}

public sealed class PdfProcessingResult
{
    public string FormType { get; set; } = string.Empty;

    /// <summary>
    /// Arbitrary strongly typed or anonymous object representing extracted data.
    /// </summary>
    public object Data { get; set; } = new { };

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(new
        {
            formType = FormType,
            data = Data
        }, options);
    }
}
