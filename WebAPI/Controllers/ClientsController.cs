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

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        await _clientRepo.AddAsync(client);
        await _clientRepo.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = client.Id }, client);
    }
}
