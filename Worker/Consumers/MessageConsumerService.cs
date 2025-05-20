using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Worker.Dtos;
using Worker.Workers;

namespace Worker.Consumers;

public class MessageConsumerService(EmailWorkerService emailWorkerService) : BackgroundService
{
    private readonly EmailWorkerService _emailWorkerService = emailWorkerService;

    private IConnection? _connection;
    private IChannel? _channel;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.QueueDeclareAsync(queue: Constants.MessageQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is not null && _channel.IsOpen)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = JsonSerializer.Deserialize<MessageDto>(Encoding.UTF8.GetString(body));
                if (message is not null)
                {
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);

                    await _emailWorkerService.ProcessAsync(Encoding.UTF8.GetString(body));
                }
            };

            await _channel.BasicConsumeAsync(queue: Constants.MessageQueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null && _connection.IsOpen)
        {
            await _connection.CloseAsync(cancellationToken);
        }
        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.CloseAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}
