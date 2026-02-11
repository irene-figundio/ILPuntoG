using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjectsController : BaseController
{
    private readonly IProjectRepository _repository;

    public ProjectsController(IProjectRepository repository, ApplicationDbContext context) : base(context)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects(int? branchId)
    {
        var allowedBranchIds = await GetUserBranchIdsAsync();

        if (branchId.HasValue)
        {
            if (!allowedBranchIds.Contains(branchId.Value))
            {
                return Forbid();
            }
            return Ok(await _repository.GetProjectsByBranchAsync(branchId.Value));
        }

        var allProjects = await _repository.GetAllAsync();
        return Ok(allProjects.Where(p => allowedBranchIds.Contains(p.BranchId)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await _repository.GetByIdAsync(id);
        if (project == null) return NotFound();

        if (!await CanAccessBranchAsync(project.BranchId))
        {
            return Forbid();
        }

        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject(Project project)
    {
        if (!IsSuperAdmin && !await CanAccessBranchAsync(project.BranchId))
        {
            return Forbid();
        }

        await _repository.AddAsync(project);
        await _repository.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(int id, Project project)
    {
        if (id != project.Id) return BadRequest();

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (!IsSuperAdmin && !await CanAccessBranchAsync(existing.BranchId))
        {
            return Forbid();
        }

        // Also check if they are trying to move it to a branch they don't have access to
        if (existing.BranchId != project.BranchId && !await CanAccessBranchAsync(project.BranchId))
        {
            return Forbid();
        }

        _repository.Update(project);
        await _repository.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _repository.GetByIdAsync(id);
        if (project == null) return NotFound();

        if (!IsSuperAdmin && !await CanAccessBranchAsync(project.BranchId))
        {
            return Forbid();
        }

        _repository.Remove(project);
        await _repository.SaveChangesAsync();
        return NoContent();
    }
}
