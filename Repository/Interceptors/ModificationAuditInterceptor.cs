using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interceptors
{
    public class ModificationAuditInterceptor : SaveChangesInterceptor
    {
        private readonly Func<int?> _getUserId;

        public ModificationAuditInterceptor(Func<int?> getUserId)
        {
            _getUserId = getUserId;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            var userId = _getUserId();

            if (eventData.Context == null)
                return base.SavingChanges(eventData, result);

            foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = userId;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = userId;
                }
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
