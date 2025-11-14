using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PdfReader.Api.Data;
using PdfReader.Api.Infrastructure;
using PdfReader.Api.Options;
using PdfReader.Core;
using Serilog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Serilog
builder.Services.AddLogging();
builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
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

// Infrastructure
builder.Services.AddScoped<IDocumentStorage, FileSystemDocumentStorage>();
builder.Services.AddSingleton<IQueueClient, RabbitMqQueueClient>();

// Processing pipeline
builder.Services.AddScoped<IPdfProcessingPipeline, StubPdfProcessingPipeline>();

// Worker
builder.Services.AddHostedService<DocumentProcessingWorker>();

var host = builder.Build();

await host.RunAsync();
