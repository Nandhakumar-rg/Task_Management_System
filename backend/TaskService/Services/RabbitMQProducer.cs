using RabbitMQ.Client;
using System;
using System.Text;
using Newtonsoft.Json;

public class RabbitMQProducer
{
    private readonly IModel _channel;

    public RabbitMQProducer(IModel channel)
    {
        _channel = channel;

        // Ensure queue exists
        _channel.QueueDeclare(queue: "task_queue",
                              durable: false,
                              exclusive: false,
                              autoDelete: false,
                              arguments: null);
    }

    public void SendMessage(string eventType, object data)
    {
        var message = new { Type = eventType, Payload = data };
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        _channel.BasicPublish(exchange: "",
                              routingKey: "task_queue",
                              basicProperties: null,
                              body: body);

        Console.WriteLine($" [x] Sent {eventType}: {JsonConvert.SerializeObject(data)}");
    }
}
