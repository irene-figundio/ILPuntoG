namespace Models;

public enum ProjectStatus
{
    Planned,
    InProgress,
    Completed,
    OnHold
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum TodoStatus
{
    Pending,
    InProgress,
    Completed
}

public enum SyncStatus
{
    NotSynced,
    Synced,
    Failed
}
