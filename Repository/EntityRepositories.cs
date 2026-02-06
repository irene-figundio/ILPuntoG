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

    public override async Task<Branch?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(b => b.UserBranches)
                .ThenInclude(ub => ub.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }
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

    public override async Task<IEnumerable<Appointment>> GetAllAsync()
    {
        return await _dbSet.Include(a => a.Branch).Include(a => a.Project).ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByBranchAsync(int branchId)
    {
        return await _dbSet.Include(a => a.Branch).Include(a => a.Project).Where(a => a.BranchId == branchId).ToListAsync();
    }
}

public interface ITodoTaskRepository : IRepository<TodoTask>
{
    Task<IEnumerable<TodoTask>> GetTasksByProjectAsync(int projectId);
}

public class TodoTaskRepository : GenericRepository<TodoTask>, ITodoTaskRepository
{
    public TodoTaskRepository(ApplicationDbContext context) : base(context) { }

    //public override async Task<IEnumerable<TodoTask>> GetAllAsync()
    //{
    //    return await _dbSet
    //        //.Include(t => t.Priority)
    //        //.Include(t => t.TaskType)
    //        //.Include(t => t.Client)
    //        //.Include(t => t.Project)
    //        //.Include(t => t.TaskAssignments)
    //        //    .ThenInclude(ta => ta.User)
    //        .ToListAsync();
    //}

    public async Task<IEnumerable<TodoTask>> GetTasksByProjectAsync(int projectId)
    {
        return await _dbSet
            .Include(t => t.Priority)
            .Include(t => t.TaskType)
            .Include(t => t.Client)
            .Include(t => t.Project)
            .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
            .Where(t => t.ProjectId == projectId).ToListAsync();
    }

    public override async Task<TodoTask?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Include(t => t.Priority)
            .Include(t => t.TaskType)
            .Include(t => t.Client)
            .Include(t => t.Project)
            .Include(t => t.TaskAssignments)
                .ThenInclude(ta => ta.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
}

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);
    }
}

public interface IRoleRepository : IRepository<Role> { }
public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext context) : base(context) { }
}

public interface IClientRepository : IRepository<Client> { }
public class ClientRepository : GenericRepository<Client>, IClientRepository
{
    public ClientRepository(ApplicationDbContext context) : base(context) { }
}

public interface ITaskTypeRepository : IRepository<TaskType> { }
public class TaskTypeRepository : GenericRepository<TaskType>, ITaskTypeRepository
{
    public TaskTypeRepository(ApplicationDbContext context) : base(context) { }
}

public interface IPriorityRepository : IRepository<Priority> { }
public class PriorityRepository : GenericRepository<Priority>, IPriorityRepository
{
    public PriorityRepository(ApplicationDbContext context) : base(context) { }
}

public interface IWorkLogRepository : IRepository<WorkLog> { }
public class WorkLogRepository : GenericRepository<WorkLog>, IWorkLogRepository
{
    public WorkLogRepository(ApplicationDbContext context) : base(context) { }
    public override async Task<IEnumerable<WorkLog>> GetAllAsync()
    {
        return await _dbSet.Include(w => w.User).Include(w => w.Documents).ToListAsync();
    }
}

public interface IUserBranchRepository : IRepository<UserBranch> { }
public class UserBranchRepository : GenericRepository<UserBranch>, IUserBranchRepository
{
    public UserBranchRepository(ApplicationDbContext context) : base(context) { }
}

public interface IClientBranchRepository : IRepository<ClientBranch> { }
public class ClientBranchRepository : GenericRepository<ClientBranch>, IClientBranchRepository
{
    public ClientBranchRepository(ApplicationDbContext context) : base(context) { }
}

public interface ITaskAssignmentRepository : IRepository<TaskAssignment> { }
public class TaskAssignmentRepository : GenericRepository<TaskAssignment>, ITaskAssignmentRepository
{
    public TaskAssignmentRepository(ApplicationDbContext context) : base(context) { }
}

public interface IDocumentRepository : IRepository<Document> { }
public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
{
    public DocumentRepository(ApplicationDbContext context) : base(context) { }
}

public interface IAuditLogRepository : IRepository<AuditLog> { }
public class AuditLogRepository : GenericRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context) { }
    public override async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _dbSet.Include(a => a.User).OrderByDescending(a => a.Timestamp).ToListAsync();
    }
}
