using Microsoft.EntityFrameworkCore;
using PdfReader.Api.Data;
using PdfReader.Api.Infrastructure;
using PdfReader.Api.Options;
using PdfReader.Core;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Options
builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

// DbContext
var connectionString = builder.Configuration.GetConnectionString("Default")
                      ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Infrastructure services
builder.Services.AddScoped<IDocumentStorage, FileSystemDocumentStorage>();
builder.Services.AddSingleton<IQueueClient, RabbitMqQueueClient>();

// Controllers / endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations at startup (best effort)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapPost("/documents", async (HttpRequest request, AppDbContext db, IDocumentStorage storage, IQueueClient queue, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("multipart/form-data expected.");
    }

    var form = await request.ReadFormAsync(ct);
    var file = form.Files["file"];

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("file is required.");
    }

    var formType = form["formType"].FirstOrDefault();

    var doc = new Document
    {
        Id = Guid.NewGuid(),
        OriginalFileName = file.FileName,
        FileSize = file.Length,
        ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType,
        FormType = formType,
        Status = DocumentStatus.Uploaded,
        CreatedAt = DateTimeOffset.UtcNow,
        StoragePath = $"pdf/{Guid.NewGuid():N}.pdf"
    };

    await using (var stream = file.OpenReadStream())
    {
        await storage.SavePdfAsync(doc.Id, stream, ct);
    }

    db.Documents.Add(doc);
    await db.SaveChangesAsync(ct);

    await queue.EnqueueAsync(new DocumentQueuedMessage(doc.Id), ct);

    return Results.Accepted($"/documents/{doc.Id}", new
    {
        id = doc.Id,
        status = doc.Status.ToString()
    });
})
.WithName("UploadDocument")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/documents/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
{
    var doc = await db.Documents.FindAsync(new object[] { id }, ct);
    if (doc is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        doc.Id,
        Status = doc.Status.ToString(),
        doc.FormType,
        doc.CreatedAt,
        doc.ProcessedAt,
        doc.ErrorMessage
    });
})
.WithName("GetDocumentStatus")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/documents/{id:guid}/result", async (Guid id, AppDbContext db, IDocumentStorage storage, CancellationToken ct) =>
{
    var doc = await db.Documents.FindAsync(new object[] { id }, ct);
    if (doc is null)
    {
        return Results.NotFound();
    }

    if (doc.Status is DocumentStatus.Uploaded or DocumentStatus.Processing)
    {
        return Results.Accepted();
    }

    if (doc.Status == DocumentStatus.Failed)
    {
        return Results.Problem(doc.ErrorMessage ?? "Processing failed.");
    }

    var json = await storage.GetResultJsonAsync(id, ct);
    if (json is null)
    {
        return Results.NotFound("Result not found.");
    }

    return Results.Content(json, "application/json");
})
.WithName("GetDocumentResult")
.Produces(StatusCodes.Status200OK, contentType: "application/json")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status404NotFound);

app.Run();
