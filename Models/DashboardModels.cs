using System.Collections.Generic;

namespace Models;

public class DashboardStats
{
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalProjects { get; set; }
    public int UpcomingAppointments { get; set; }
}

public class BranchSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClientCount { get; set; }
    public int ProjectCount { get; set; }
    public int TaskCount { get; set; }
    public List<string> Team { get; set; } = new();
}
