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

    public BranchesController(IBranchRepository repository)
    {
        _repository = repository;
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
    public async Task<ActionResult<Branch>> CreateBranch(Branch branch)
    {
        await _repository.AddAsync(branch);
        await _repository.SaveChangesAsync();
        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBranch(int id, Branch branch)
    {
        if (id != branch.Id)
        {
            return BadRequest();
        }
        _repository.Update(branch);
        await _repository.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBranch(int id)
    {
        var branch = await _repository.GetByIdAsync(id);
        if (branch == null)
        {
            return NotFound();
        }
        _repository.Remove(branch);
        await _repository.SaveChangesAsync();
        return NoContent();
    }
}
