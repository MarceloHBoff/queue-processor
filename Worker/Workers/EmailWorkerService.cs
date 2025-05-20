using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System.Text;
using Worker.Interface;

namespace Worker.Workers;

public class EmailWorkerService : IHostedService, IWorker
{
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(queue: Constants.EmailQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
    }

    public async Task ProcessAsync(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.BasicPublishAsync(exchange: "", routingKey: Constants.EmailQueueName, body: body);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _connection.IsOpen)
        {
            await _connection.CloseAsync(cancellationToken);
        }
        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.CloseAsync(cancellationToken);
        }
    }
}
