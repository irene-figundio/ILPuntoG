namespace Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // User, Admin, SuperAdmin
}

public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
}
