using System;

namespace Models;

public class Appointment
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Duration { get; set; }

    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public int BranchId { get; set; }
    public Branch? Branch { get; set; }
}
