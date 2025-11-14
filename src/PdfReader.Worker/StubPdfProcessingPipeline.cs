using PdfReader.Core;

namespace PdfReader.Worker;

/// <summary>
/// Stub implementation of IPdfProcessingPipeline.
/// In real system this should:
/// 1. Try high-quality PDF parsing using a library.
/// 2. If low confidence, call AI/OCR fallback.
/// </summary>
public sealed class StubPdfProcessingPipeline : IPdfProcessingPipeline
{
    /// <inheritdoc/>
    public Task<PdfProcessingResult> ProcessAsync(Stream pdf, string? formType, CancellationToken ct = default)
    {
        // NOTE: This is just a placeholder. It does NOT inspect the PDF.
        // Replace with real implementation (PdfPig / commercial PDF lib + AI fallback) later.

        var result = new PdfProcessingResult
        {
            FormType = string.IsNullOrWhiteSpace(formType) ? "UnknownForm" : formType,
            Data = new
            {
                processedAt = DateTimeOffset.UtcNow,
                note = "This is a stub implementation. Replace with real PDF parsing and AI fallback."
            }
        };

        return Task.FromResult(result);
    }
}
