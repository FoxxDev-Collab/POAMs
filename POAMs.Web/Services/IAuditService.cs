using POAMs.Web.Models.Domain;

namespace POAMs.Web.Services;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityType, int? entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null);
    Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null, string? entityType = null, int? entityId = null);
    Task<IEnumerable<AuditLog>> GetUserLogsAsync(int userId, int count = 50);
}
