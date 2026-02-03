using System.Collections.Generic;

namespace Models;

public class Project : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }

    public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; } = null;
    public DateTime? ModifiedAt { get; set; } = null;
    public int? ModifiedBy { get; set; } = null;

}
