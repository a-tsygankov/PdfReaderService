using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PdfReader.Api.Options;
using PdfReader.Core;
using RabbitMQ.Client;

namespace PdfReader.Api.Infrastructure;

public sealed class RabbitMqQueueClient : IQueueClient, IAsyncDisposable
{
    private readonly QueueOptions _options;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly ValueTask<IConnection> _connectionTask;

    public RabbitMqQueueClient(IOptions<QueueOptions> options)
    {
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            ClientProvidedName = "pdfreader-api",
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };

        // Safe connection creation with internal CTS (tied to DisposeAsync)
        _connectionTask = new ValueTask<IConnection>(factory.CreateConnectionAsync(_disposeCts.Token));
    }

private async Task<IChannel> CreateChannelAsync(CancellationToken ct)
{
    var conn = await _connectionTask;
    var options = new CreateChannelOptions(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: false,
        outstandingPublisherConfirmationsRateLimiter: null,
        consumerDispatchConcurrency: 1
    );

    return await conn.CreateChannelAsync(
        options: options,
        cancellationToken: ct
    );
}


    public async Task EnqueueAsync(DocumentQueuedMessage message, CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
        var token = linkedCts.Token;

        await using var channel = await CreateChannelAsync(token);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: token
        );

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: token
        );
    }

    public async Task<DocumentQueuedMessage?> DequeueAsync(CancellationToken ct = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
        var token = linkedCts.Token;

        await using var channel = await CreateChannelAsync(token);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: token
        );

        var result = await channel.BasicGetAsync(
            _options.QueueName,
            autoAck: true,
            cancellationToken: token
        );

        if (result is null)
            return null;

        var json = Encoding.UTF8.GetString(result.Body.ToArray());
        return JsonSerializer.Deserialize<DocumentQueuedMessage>(json);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _disposeCts.Cancel();

            if (_connectionTask.IsCompletedSuccessfully)
            {
                var conn = await _connectionTask;
                await conn.DisposeAsync();
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            _disposeCts.Dispose();
        }
    }
}
