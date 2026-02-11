using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ClientsController : BaseController
{
    private readonly IClientRepository _clientRepo;

    public ClientsController(IClientRepository clientRepo, ApplicationDbContext context) : base(context)
    {
        _clientRepo = clientRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int? branchId = null)
    {
        var allowedIds = await GetUserBranchIdsAsync();
        var clients = await _clientRepo.GetAllAsync();

        // Filter clients by allowed branches
        clients = clients.Where(c => c.ClientBranches.Any(cb => allowedIds.Contains(cb.BranchId)));

        if (branchId.HasValue)
        {
            if (!allowedIds.Contains(branchId.Value)) return Forbid();
            clients = clients.Where(c => c.ClientBranches.Any(cb => cb.BranchId == branchId.Value));
        }

        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) return NotFound();

        var allowedIds = await GetUserBranchIdsAsync();
        if (!client.ClientBranches.Any(cb => allowedIds.Contains(cb.BranchId)))
        {
            return Forbid();
        }

        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        // For simple Admin/User, we must ensure they are adding the client to a branch they can access
        // If it's a new client without branches yet, maybe we allow it if we assign them to user's branch

        await _clientRepo.AddAsync(client);
        await _clientRepo.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Client client)
    {
        if (id != client.Id) return BadRequest();

        var existing = await _clientRepo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var allowedIds = await GetUserBranchIdsAsync();
        if (!existing.ClientBranches.Any(cb => allowedIds.Contains(cb.BranchId)))
        {
            return Forbid();
        }

        _clientRepo.Update(client);
        await _clientRepo.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) return NotFound();

        var allowedIds = await GetUserBranchIdsAsync();
        if (!client.ClientBranches.Any(cb => allowedIds.Contains(cb.BranchId)))
        {
            return Forbid();
        }

        _clientRepo.Remove(client);
        await _clientRepo.SaveChangesAsync();
        return NoContent();
    }
}
