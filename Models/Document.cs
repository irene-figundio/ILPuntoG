using System;

namespace Models;

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; } = DateTime.Now;

    public int? WorkLogId { get; set; }
    public WorkLog? WorkLog { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
}
