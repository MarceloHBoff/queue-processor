using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Worker.Dtos;

namespace Worker.Consumers;

public class EmailConsumerService(EmailSenderService emailSenderService, GroqService groqService) : BackgroundService
{
    private readonly EmailSenderService _emailSenderService = emailSenderService;
    private readonly GroqService _groqService = groqService;

    private IConnection? _connection;
    private IChannel? _channel;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        if (_channel is not null && _channel.IsOpen)
        {
            await _channel.QueueDeclareAsync(queue: Constants.EmailQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
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

                var response = await _groqService.GetResponse(message?.Text ?? "");

                await _emailSenderService.SendEmail(message?.Email ?? "", response, message?.Text ?? "");

                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await _channel.BasicConsumeAsync(queue: Constants.EmailQueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
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
