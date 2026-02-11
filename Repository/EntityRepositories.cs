using Microsoft.EntityFrameworkCore;
using Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repository;

public interface IBranchRepository : IRepository<Branch> { }

public class BranchRepository : GenericRepository<Branch>, IBranchRepository
{
    public BranchRepository(ApplicationDbContext context) : base(context) { }
}

public interface IProjectRepository : IRepository<Project>
{
    Task<IEnumerable<Project>> GetProjectsByBranchAsync(int branchId);
}

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Project>> GetProjectsByBranchAsync(int branchId)
    {
        return await _dbSet.Where(p => p.BranchId == branchId).ToListAsync();
    }
}

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<IEnumerable<Appointment>> GetAppointmentsByBranchAsync(int branchId);
}

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByBranchAsync(int branchId)
    {
        return await _dbSet.Where(a => a.BranchId == branchId).ToListAsync();
    }
}

public interface ITodoTaskRepository : IRepository<TodoTask>
{
    Task<IEnumerable<TodoTask>> GetTasksByProjectAsync(int projectId);
}

public class TodoTaskRepository : GenericRepository<TodoTask>, ITodoTaskRepository
{
    public TodoTaskRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TodoTask>> GetTasksByProjectAsync(int projectId)
    {
        return await _dbSet.Where(t => t.ProjectId == projectId).ToListAsync();
    }
}

public interface IUserRepository : IRepository<User> { }

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }
}
