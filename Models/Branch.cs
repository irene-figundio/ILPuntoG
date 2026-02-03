using System.Collections.Generic;

namespace Models;

public class Branch : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; } = null;
    public DateTime? ModifiedAt { get; set; } = null;
    public int? ModifiedBy { get; set; } = null;
}
