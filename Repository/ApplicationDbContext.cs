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
            // Default to SQLite if not configured (e.g. during migrations)
            optionsBuilder.UseSqlite("Data Source=IlPuntoG.db");
        }
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
    public DbSet<Role> Roles { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<TaskType> TaskTypes { get; set; }
    public DbSet<Priority> Priorities { get; set; }
    public DbSet<WorkLog> WorkLogs { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<GoogleCredential> GoogleCredentials { get; set; }
    public DbSet<UserBranch> UserBranches { get; set; }
    public DbSet<ClientBranch> ClientBranches { get; set; }
    public DbSet<TaskAssignment> TaskAssignments { get; set; }

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

        // Many-to-many User-Branch
        modelBuilder.Entity<UserBranch>()
            .HasKey(ub => new { ub.UserId, ub.BranchId });
        modelBuilder.Entity<UserBranch>()
            .HasOne(ub => ub.User)
            .WithMany(u => u.UserBranches)
            .HasForeignKey(ub => ub.UserId);
        modelBuilder.Entity<UserBranch>()
            .HasOne(ub => ub.Branch)
            .WithMany(b => b.UserBranches)
            .HasForeignKey(ub => ub.BranchId);

        // Many-to-many Client-Branch
        modelBuilder.Entity<ClientBranch>()
            .HasKey(cb => new { cb.ClientId, cb.BranchId });
        modelBuilder.Entity<ClientBranch>()
            .HasOne(cb => cb.Client)
            .WithMany(c => c.ClientBranches)
            .HasForeignKey(cb => cb.ClientId);
        modelBuilder.Entity<ClientBranch>()
            .HasOne(cb => cb.Branch)
            .WithMany(b => b.ClientBranches)
            .HasForeignKey(cb => cb.BranchId);

        // Many-to-many TodoTask-User (Assignments)
        modelBuilder.Entity<TaskAssignment>()
            .HasKey(ta => new { ta.TodoTaskId, ta.UserId });
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(ta => ta.TodoTask)
            .WithMany(t => t.TaskAssignments)
            .HasForeignKey(ta => ta.TodoTaskId);
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(ta => ta.User)
            .WithMany(u => u.TaskAssignments)
            .HasForeignKey(ta => ta.UserId);

        modelBuilder.Entity<GoogleCredential>()
            .HasOne(g => g.User)
            .WithMany()
            .HasForeignKey(g => g.UserId);

        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = Models.Roles.User },
            new Role { Id = 2, Name = Models.Roles.Admin },
            new Role { Id = 3, Name = Models.Roles.SuperAdmin }
        );

        // Seed Priorities
        modelBuilder.Entity<Priority>().HasData(
            new Priority { Id = 1, Name = "Bassa", LevelId = 1 },
            new Priority { Id = 2, Name = "Media", LevelId = 2 },
            new Priority { Id = 3, Name = "Alta", LevelId = 3 },
            new Priority { Id = 4, Name = "Urgente", LevelId = 4 }
        );

        // Seed TaskTypes
        modelBuilder.Entity<TaskType>().HasData(
            new TaskType { Id = 1, Name = "Sopralluogo", IsBase = true },
            new TaskType { Id = 2, Name = "Riparazione", IsBase = true },
            new TaskType { Id = 3, Name = "Installazione", IsBase = true },
            new TaskType { Id = 4, Name = "Manutenzione", IsBase = true },
            new TaskType { Id = 5, Name = "Sviluppo", IsBase = false }
        );
    }
}
