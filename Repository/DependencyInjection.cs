using Microsoft.Extensions.DependencyInjection;
using Repository.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<ModificationAuditInterceptor>();

            services.AddScoped<IBranchRepository, BranchRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            services.AddScoped<ITodoTaskRepository, TodoTaskRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            return services;
        }
    }
}
