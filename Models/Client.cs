using System.Collections.Generic;

namespace Models;

public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    public List<ClientBranch> ClientBranches { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}
