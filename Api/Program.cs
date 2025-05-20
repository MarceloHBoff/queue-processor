using System.Text.Json;
using Worker;
using Worker.Consumers;
using Worker.Dtos;
using Worker.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<EmailSenderService>();
builder.Services.AddSingleton<EmailWorkerService>();
builder.Services.AddSingleton<GroqService>();
builder.Services.AddSingleton<MessageWorkerService>();

builder.Services.AddHostedService(provider => provider.GetRequiredService<MessageWorkerService>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<EmailWorkerService>());

builder.Services.AddHostedService<EmailConsumerService>();
builder.Services.AddHostedService<MessageConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/send", async (MessageDto msg, MessageWorkerService messageWorkerService) =>
{
    await messageWorkerService.ProcessAsync(JsonSerializer.Serialize(msg));

    return Results.Ok("Ok");
});

app.Run();
