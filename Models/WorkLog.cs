using System;

namespace Models;

public enum WorkLogType
{
    Work,
    Holiday, // Ferie
    Permit,  // Permessi
    Sickness // Malattia
}

public class WorkLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime Date { get; set; }
    public double Hours { get; set; }
    public WorkLogType Type { get; set; }
}
