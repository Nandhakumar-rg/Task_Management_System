using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using Newtonsoft.Json;

class NotificationService
{
    public static void Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        //  Ensure queue name matches the producer
        string queueName = "task_queue"; // Producer must use the same queue name

        channel.QueueDeclare(queue: queueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                Console.WriteLine($" [NotificationService] Raw Received Message: {message}");

                // Deserialize into a strongly-typed object
                var taskEvent = JsonConvert.DeserializeObject<TaskEvent>(message);

                if (taskEvent != null && !string.IsNullOrEmpty(taskEvent.Type))
                {
                    Console.WriteLine($" [NotificationService] Event Type: {taskEvent.Type}");

                    switch (taskEvent.Type)
                    {
                        case "TaskCreated":
                            Console.WriteLine($"[+] New Task Created: {taskEvent.Payload.Title}");
                            break;

                        case "TaskUpdated":
                            Console.WriteLine($"[~] Task Updated: {taskEvent.Payload.Title}");
                            break;

                        case "TaskDeleted":
                            Console.WriteLine($"[-] Task Deleted with ID: {taskEvent.Payload.Id}");
                            break;

                        default:
                            Console.WriteLine($"[!] Unknown Event Type: {taskEvent.Type}");
                            break;
                    }
                }
                else
                {
                    Console.WriteLine($" [NotificationService] Invalid event received: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" [NotificationService] Error processing message: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: queueName,
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine(" [*] Waiting for messages... Press [Ctrl+C] to exit.");
        while (true) { } // Keep the service running
    }
}

// Strongly-typed model for deserialization
public class TaskEvent
{
    public string Type { get; set; }
    public TaskPayload Payload { get; set; }
}

public class TaskPayload
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
}
