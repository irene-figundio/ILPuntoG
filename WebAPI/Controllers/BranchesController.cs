using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly IBranchRepository _repository;
    private readonly WebAPI.Services.AuditService _auditService;

    public BranchesController(IBranchRepository repository, WebAPI.Services.AuditService auditService)
    {
        _repository = repository;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Branch>>> GetBranches()
    {
        var branches = await _repository.GetAllAsync();
        return Ok(branches);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Branch>> GetBranch(int id)
    {
        var branch = await _repository.GetByIdAsync(id);
        if (branch == null)
        {
            return NotFound();
        }
        return Ok(branch);
    }

    [HttpPost]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<ActionResult<Branch>> CreateBranch(Branch branch)
    {
        await _repository.AddAsync(branch);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Create", "Branch", branch.Id.ToString(), branch.Name);
        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> UpdateBranch(int id, Branch branch)
    {
        if (id != branch.Id) return BadRequest();
        _repository.Update(branch);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Update", "Branch", branch.Id.ToString(), branch.Name);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> DeleteBranch(int id)
    {
        var branch = await _repository.GetByIdAsync(id);
        if (branch == null) return NotFound();
        _repository.Remove(branch);
        await _repository.SaveChangesAsync();
        await _auditService.LogAsync("Delete", "Branch", id.ToString(), branch.Name);
        return NoContent();
    }
}
