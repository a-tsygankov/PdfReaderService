using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PdfReader.Api.Options;
using PdfReader.Core;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PdfReader.Api.Infrastructure;

public sealed class RabbitMqQueueClient : IQueueClient, IDisposable
{
    private readonly QueueOptions _options;
    private readonly Lazy<IConnection> _connectionLazy;

    public RabbitMqQueueClient(IOptions<QueueOptions> options)
    {
        _options = options.Value;
        _connectionLazy = new Lazy<IConnection>(CreateConnection);
    }

    private IConnection Connection => _connectionLazy.Value;

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection("pdfreader-api");
    }

    public async Task EnqueueAsync(DocumentQueuedMessage message, CancellationToken ct = default)
    {
        using var channel = Connection.CreateModel();
        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var payload = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(payload);

        var props = channel.CreateBasicProperties();
        props.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            basicProperties: props,
            body: body);

        await Task.CompletedTask;
    }

    public async Task<DocumentQueuedMessage?> DequeueAsync(CancellationToken ct = default)
    {
        using var channel = Connection.CreateModel();
        channel.QueueDeclare(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var result = channel.BasicGet(_options.QueueName, autoAck: true);
        if (result is null)
        {
            return await Task.FromResult<DocumentQueuedMessage?>(null);
        }

        var json = Encoding.UTF8.GetString(result.Body.ToArray());
        var message = JsonSerializer.Deserialize<DocumentQueuedMessage>(json);
        return message;
    }

    public void Dispose()
    {
        if (_connectionLazy.IsValueCreated)
        {
            _connectionLazy.Value.Dispose();
        }
    }
}
