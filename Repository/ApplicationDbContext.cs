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
            var basePath = Directory.GetCurrentDirectory();
            if (!File.Exists(Path.Combine(basePath, "appsettings.json")) && Directory.Exists(Path.Combine(basePath, "../WebAPI")))
            {
                basePath = Path.Combine(basePath, "../WebAPI");
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var dbCredentials = configuration.GetSection("DbCredentials");
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (dbCredentials.Exists() && !string.IsNullOrEmpty(dbCredentials["Server"]))
            {
                var sb = new System.Text.StringBuilder();
                sb.Append($"Server={dbCredentials["Server"]};");
                sb.Append($"Database={dbCredentials["Database"]};");
                if (dbCredentials.GetValue<bool>("TrustedConnection"))
                    sb.Append("Trusted_Connection=True;");
                else
                    sb.Append($"User Id={dbCredentials["UserId"]};Password={dbCredentials["Password"]};");

                sb.Append("MultipleActiveResultSets=true;TrustServerCertificate=True;");
                optionsBuilder.UseSqlServer(sb.ToString());
            }
            else if (connectionString != null)
            {
                if (connectionString.Contains("Server="))
                    optionsBuilder.UseSqlServer(connectionString);
                else
                    optionsBuilder.UseSqlite(connectionString);
            }
            else
            {
                optionsBuilder.UseSqlite("Data Source=IlPuntoG.db");
            }
        }
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
