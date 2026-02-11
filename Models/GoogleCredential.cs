using System;

namespace Models;

public class GoogleCredential
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expiry { get; set; }
    public string? CalendarEmail { get; set; }
}
