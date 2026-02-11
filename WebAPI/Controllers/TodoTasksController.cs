using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoTasksController : ControllerBase
{
    private readonly ITodoTaskRepository _repository;

    public TodoTasksController(ITodoTaskRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoTask>>> GetTasks(int? projectId)
    {
        if (projectId.HasValue)
        {
            return Ok(await _repository.GetTasksByProjectAsync(projectId.Value));
        }
        return Ok(await _repository.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoTask>> GetTask(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TodoTask>> CreateTask(TodoTask task)
    {
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(int id, TodoTask task)
    {
        if (id != task.Id)
        {
            return BadRequest();
        }
        _repository.Update(task);
        await _repository.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound();
        }
        _repository.Remove(task);
        await _repository.SaveChangesAsync();
        return NoContent();
    }
}
