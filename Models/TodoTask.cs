using System;

namespace Models;

public class TodoTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public DateTime? Deadline { get; set; }
    public TodoStatus Status { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
}
