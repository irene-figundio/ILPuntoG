namespace Models;

public class TaskAssignment
{
    public int TodoTaskId { get; set; }
    public TodoTask? TodoTask { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}
