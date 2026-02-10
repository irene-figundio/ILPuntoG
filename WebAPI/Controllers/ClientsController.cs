using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using Repository;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _clientRepo;

    public ClientsController(IClientRepository clientRepo)
    {
        _clientRepo = clientRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int? branchId = null)
    {
        var clients = await _clientRepo.GetAllAsync();
        if (branchId.HasValue)
        {
            clients = clients.Where(c => c.ClientBranches.Any(cb => cb.BranchId == branchId.Value));
        }
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) return NotFound();
        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        await _clientRepo.AddAsync(client);
        await _clientRepo.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Client client)
    {
        if (id != client.Id) return BadRequest();
        _clientRepo.Update(client);
        await _clientRepo.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _clientRepo.GetByIdAsync(id);
        if (client == null) return NotFound();
        _clientRepo.Remove(client);
        await _clientRepo.SaveChangesAsync();
        return NoContent();
    }
}
