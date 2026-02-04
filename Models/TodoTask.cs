using System;
using System.Collections.Generic;

namespace Models;

public enum TaskRecurrence
{
    None,
    Daily,
    Weekly,
    Monthly,
    Yearly
}

public class TodoTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int PriorityId { get; set; }
    public Priority? Priority { get; set; }

    public DateTime Deadline { get; set; } // Obbligatorio
    public DateTime? ProcessedAt { get; set; } // Data di elaborazione
    public TodoStatus Status { get; set; }
    public bool IsRecurring { get; set; }
    public TaskRecurrence Recurrence { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public int TaskTypeId { get; set; }
    public TaskType? TaskType { get; set; }

    public string? GitLabRepoUrl { get; set; }

    public List<TaskAssignment> TaskAssignments { get; set; } = new();
}
