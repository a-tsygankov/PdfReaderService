using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfReader.Api.Data;
using PdfReader.Core;

namespace PdfReader.Worker;

public sealed class DocumentProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    public DocumentProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<DocumentProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentProcessingWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorage>();
                var queue = scope.ServiceProvider.GetRequiredService<IQueueClient>();
                var pipeline = scope.ServiceProvider.GetRequiredService<IPdfProcessingPipeline>();

                var message = await queue.DequeueAsync(stoppingToken);
                if (message is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                var documentId = message.DocumentId;
                _logger.LogInformation("Dequeued document {DocumentId} for processing.", documentId);

                var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId, stoppingToken);
                if (doc is null)
                {
                    _logger.LogWarning("Document {DocumentId} not found in database.", documentId);
                    continue;
                }

                doc.Status = DocumentStatus.Processing;
                await db.SaveChangesAsync(stoppingToken);

                try
                {
                    await using var pdfStream = await storage.GetPdfAsync(doc.Id, stoppingToken);
                    var result = await pipeline.ProcessAsync(pdfStream, doc.FormType, stoppingToken);

                    var json = result.ToJson();
                    await storage.SaveResultJsonAsync(doc.Id, json, stoppingToken);

                    doc.Status = DocumentStatus.Succeeded;
                    doc.ProcessedAt = DateTimeOffset.UtcNow;
                    doc.ResultPath = $"json/{doc.Id:N}.json";

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Successfully processed document {DocumentId}.", documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document {DocumentId}.", documentId);
                    doc.Status = DocumentStatus.Failed;
                    doc.ErrorMessage = ex.Message;
                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in processing loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("DocumentProcessingWorker stopping.");
    }
}
