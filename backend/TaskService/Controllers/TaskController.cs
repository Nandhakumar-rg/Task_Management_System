using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[Authorize] // 🔹 Secure API Endpoints
[Route("api/[controller]")]
[ApiController]
public class TaskController : ControllerBase
{
    private readonly AppDbContext _context;
    public TaskController(AppDbContext context) { _context = context; }

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
    }

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
}
