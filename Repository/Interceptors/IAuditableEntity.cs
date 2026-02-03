using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interceptors
{
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        int? CreatedBy { get; set; }

        DateTime? ModifiedAt { get; set; }
        int? ModifiedBy { get; set; }
    }
}
