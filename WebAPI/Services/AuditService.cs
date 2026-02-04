using Models;
using Repository;
using System.Security.Claims;

namespace WebAPI.Services;

public class AuditService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(IAuditLogRepository auditLogRepository, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogRepository = auditLogRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityName, string? entityId, string? details = null, int? userId = null)
    {
        if (userId == null)
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            userId = int.TryParse(userIdStr, out var id) ? id : null;
        }

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.Now
        };

        await _auditLogRepository.AddAsync(log);
        await _auditLogRepository.SaveChangesAsync();
    }
}
