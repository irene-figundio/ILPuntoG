using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repository;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "super_secret_key_that_is_long_enough_for_sha256");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dbCredentials = builder.Configuration.GetSection("DbCredentials");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
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
        options.UseSqlServer(sb.ToString());
    }
    else if (connectionString != null)
    {
        if (connectionString.Contains("Server="))
            options.UseSqlServer(connectionString);
        else
            options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlite("Data Source=IlPuntoG.db");
    }
});

builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<ITodoTaskRepository, TodoTaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

var app = builder.Build();

// Create database if not exists
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
