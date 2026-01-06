using Microsoft.EntityFrameworkCore;
using POAMs.Web.Data;
using POAMs.Web.Models.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace POAMs.Web.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? userId, string action, string entityType, int? entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, _jsonOptions) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues, _jsonOptions) : null,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null, string? entityType = null, int? entityId = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value);

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(1000)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetUserLogsAsync(int userId, int count = 50)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
