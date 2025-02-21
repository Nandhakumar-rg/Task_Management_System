using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client;

[Route("api/[controller]")]
[ApiController]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IDatabase _cache;
    private readonly IModel _channel;
    private readonly RabbitMQProducer _rabbitMQProducer;

    public TaskController(AppDbContext context, IConnectionMultiplexer redis, IModel channel, RabbitMQProducer rabbitMQProducer)
    {
        _context = context;
        _cache = redis.GetDatabase();
        _channel = channel; //  FIXED: Initialize RabbitMQ channel
        _rabbitMQProducer = rabbitMQProducer;
    }

    //  Publish Message to RabbitMQ
    private void PublishMessage(string messageType, object data)
    {
        var taskEvent = new
        {
            Type = messageType,
            Payload = data // Ensure this is a TaskItem object
        };

        string jsonMessage = JsonSerializer.Serialize(taskEvent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var body = Encoding.UTF8.GetBytes(jsonMessage);
        _channel.BasicPublish(exchange: "",
                              routingKey: "task_queue",
                              basicProperties: null,
                              body: body);

        Console.WriteLine($" [TaskService] Sent Event: {messageType} => {jsonMessage}");
    }




    //  Get All Tasks (with Redis Caching)


    /* //This used to JWT token validated, I'm holding this temporairly
     
      [HttpGet] 
     public async Task<IActionResult> GetTasks()
     {
         var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
         if (userId == null) return Unauthorized();

         var tasks = await _context.Tasks.Where(t => t.UserId == userId).ToListAsync();
         return Ok(tasks);
     }

     [HttpPost] 
     public async Task<IActionResult> AddTask([FromBody] TaskItem task)
     {
         var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Get User ID from JWT
         if (userId == null) return Unauthorized();

         task.UserId = userId; // Automatically assign User ID
         _context.Tasks.Add(task);
         await _context.SaveChangesAsync();

         return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
     

        [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskItem updatedTask)
    {
        if (id != updatedTask.Id) return BadRequest();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var existingTask = await _context.Tasks.FindAsync(id);
        if (existingTask == null || existingTask.UserId != userId) return NotFound();

        existingTask.Title = updatedTask.Title;
        existingTask.Description = updatedTask.Description;
        existingTask.IsCompleted = updatedTask.IsCompleted;


        _context.Tasks.Update(existingTask);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.UserId != userId) return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
     */


    //  Get All Tasks (with Redis Caching)
    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        string cacheKey = "taskList";
        string cachedData = await _cache.StringGetAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            Console.WriteLine("Fetching from cache...");
            var cachedTasks = JsonSerializer.Deserialize<List<TaskItem>>(cachedData);
            return Ok(cachedTasks);
        }

        Console.WriteLine("Fetching from database...");
        var tasks = await _context.Tasks.ToListAsync();

        if (tasks.Any())
        {
            await _cache.StringSetAsync(cacheKey, JsonSerializer.Serialize(tasks), TimeSpan.FromMinutes(5));
        }

        return Ok(tasks);
    }


    //  Create a Task (Invalidate Cache + Send Event)
    [HttpPost]
    public async Task<IActionResult> AddTask([FromBody] TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Send message to RabbitMQ after task creation
        PublishMessage("TaskCreated", task);

        // Invalidate cache since new data is added
        await _cache.KeyDeleteAsync("taskList");

        return CreatedAtAction(nameof(GetTasks), new { id = task.Id }, task);
    }

    //  Update a Task (Invalidate Cache + Send Event)
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskItem updatedTask)
    {
        if (id != updatedTask.Id) return BadRequest();

        var existingTask = await _context.Tasks.FindAsync(id);
        if (existingTask == null) return NotFound();

        existingTask.Title = updatedTask.Title;
        existingTask.Description = updatedTask.Description;
        existingTask.IsCompleted = updatedTask.IsCompleted;

        _context.Tasks.Update(existingTask);
        await _context.SaveChangesAsync();

        // Send message to RabbitMQ after task update
        PublishMessage("TaskUpdated", existingTask);

        // Invalidate cache since data is updated
        await _cache.KeyDeleteAsync("taskList");

        return NoContent();
    }

    //  Delete a Task (Invalidate Cache + Send Event)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        // Send message to RabbitMQ after task deletion
        PublishMessage("TaskDeleted", new { Id = id });


        // Invalidate cache since data is deleted
        await _cache.KeyDeleteAsync("taskList");

        return NoContent();
    }
}



