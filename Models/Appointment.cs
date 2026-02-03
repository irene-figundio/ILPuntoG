using System;

namespace Models;

public class Appointment : IAuditableEntity
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; } = null;
    public DateTime? ModifiedAt { get; set; } = null;
    public int? ModifiedBy { get; set; } = null;
}
