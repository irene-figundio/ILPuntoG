using System.Collections.Generic;

namespace Models;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string HexColor { get; set; } = "#3498db"; // Default color
    public string? GoogleCalendarId { get; set; }

    // Navigation properties
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public List<UserBranch> UserBranches { get; set; } = new();
    public List<ClientBranch> ClientBranches { get; set; } = new();
}
