namespace Models;

public class TaskType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsBase { get; set; } = true;
}
