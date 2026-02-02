using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models;
using System.IO;

namespace Repository;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var configuration = BuildConfiguration();
            var connectionString = GetConnectionString(configuration);

            if (connectionString.Contains("Server="))
            {
                optionsBuilder.UseSqlServer(connectionString);
            }
            else
            {
                optionsBuilder.UseSqlite(connectionString);
            }
        }
    }

    private IConfiguration BuildConfiguration()
    {
        var basePath = Directory.GetCurrentDirectory();

        // Robust search for appsettings.json
        var current = new DirectoryInfo(basePath);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "appsettings.json")))
        {
            var webApiDir = Path.Combine(current.FullName, "WebAPI");
            if (Directory.Exists(webApiDir) && File.Exists(Path.Combine(webApiDir, "appsettings.json")))
            {
                basePath = webApiDir;
                break;
            }
            current = current.Parent;
        }

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private string GetConnectionString(IConfiguration configuration)
    {
        var dbCredentials = configuration.GetSection("DbCredentials");
        if (dbCredentials.Exists() && !string.IsNullOrEmpty(dbCredentials["Server"]))
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Server={dbCredentials["Server"]};");
            sb.Append($"Database={dbCredentials["Database"] ?? "IlPuntoG"};");

            if (dbCredentials.GetValue<bool>("TrustedConnection"))
            {
                sb.Append("Trusted_Connection=True;");
            }
            else
            {
                sb.Append($"User Id={dbCredentials["UserId"]};Password={dbCredentials["Password"]};");
            }

            sb.Append("MultipleActiveResultSets=true;TrustServerCertificate=True;");
            return sb.ToString();
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return connectionString ?? "Data Source=IlPuntoG.db";
    }

    public DbSet<Branch> Branches { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<TodoTask> TodoTasks { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Branch)
            .WithMany(b => b.Projects)
            .HasForeignKey(p => p.BranchId);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Branch)
            .WithMany(b => b.Appointments)
            .HasForeignKey(a => a.BranchId);
    }
}
