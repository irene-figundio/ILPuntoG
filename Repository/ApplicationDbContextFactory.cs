using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Repository;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Look for appsettings.json in the WebAPI project
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebAPI"))
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
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
            builder.UseSqlServer(sb.ToString());
        }
        else if (connectionString != null)
        {
            if (connectionString.Contains("Server="))
                builder.UseSqlServer(connectionString);
            else
                builder.UseSqlite(connectionString);
        }
        else
        {
            builder.UseSqlite("Data Source=IlPuntoG.db");
        }

        return new ApplicationDbContext(builder.Options);
    }
}
