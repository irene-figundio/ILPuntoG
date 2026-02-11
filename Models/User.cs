using System.Collections.Generic;

namespace Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public List<UserBranch> UserBranches { get; set; } = new();
    public List<TaskAssignment> TaskAssignments { get; set; } = new();
    public List<WorkLog> WorkLogs { get; set; } = new();
}
