using System;

namespace Models;

public class Vacation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Note { get; set; }
    public bool IsApproved { get; set; }
    public string? GoogleEventId { get; set; }
}
