using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

public class RabbitMQConsumer
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQConsumer()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(queue: "task_queue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
    }

    public void StartConsuming()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {message}");
        };

        _channel.BasicConsume(queue: "task_queue",
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine(" [*] Waiting for messages. Press [Enter] to exit.");
        Task.Run(() => Console.ReadLine()); // Keeps the program running
    }
}
