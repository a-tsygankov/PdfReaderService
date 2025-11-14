using Microsoft.Extensions.Options;
using PdfReader.Api.Options;
using PdfReader.Core;

namespace PdfReader.Api.Infrastructure;

public sealed class FileSystemDocumentStorage : IDocumentStorage
{
    private readonly StorageOptions _options;

    public FileSystemDocumentStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(GetPdfDirectory());
        Directory.CreateDirectory(GetResultDirectory());
    }

    private string GetPdfDirectory() => Path.Combine(_options.BasePath, "pdf");
    private string GetResultDirectory() => Path.Combine(_options.BasePath, "json");

    private string GetPdfPath(Guid documentId) => Path.Combine(GetPdfDirectory(), $"{documentId:N}.pdf");
    private string GetResultPath(Guid documentId) => Path.Combine(GetResultDirectory(), $"{documentId:N}.json");

    public async Task SavePdfAsync(Guid documentId, Stream pdfStream, CancellationToken ct = default)
    {
        var path = GetPdfPath(documentId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var file = File.Create(path);
        await pdfStream.CopyToAsync(file, ct);
    }

    public Task<Stream> GetPdfAsync(Guid documentId, CancellationToken ct = default)
    {
        var path = GetPdfPath(documentId);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("PDF file not found for document.", path);
        }

        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public async Task SaveResultJsonAsync(Guid documentId, string json, CancellationToken ct = default)
    {
        var path = GetResultPath(documentId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await File.WriteAllTextAsync(path, json, ct);
    }

    public async Task<string?> GetResultJsonAsync(Guid documentId, CancellationToken ct = default)
    {
        var path = GetResultPath(documentId);
        if (!File.Exists(path))
        {
            return null;
        }

        return await File.ReadAllTextAsync(path, ct);
    }
}
