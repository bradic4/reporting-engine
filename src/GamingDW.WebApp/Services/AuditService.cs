using System.Text.Json;
using GamingDW.Core.Data;
using GamingDW.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GamingDW.WebApp.Services;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, int? entityId, int? userId, string? username,
        object? oldValues = null, object? newValues = null, string? ipAddress = null);
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(string? entityType, int? entityId, string? username,
        string? fromDate, string? toDate, int page = 1, int pageSize = 20);
}

public class AuditService : IAuditService
{
    private readonly GamingDbContext _db;
    private readonly ILogger<AuditService> _logger;

    public AuditService(GamingDbContext db, ILogger<AuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(string action, string entityType, int? entityId, int? userId, string? username,
        object? oldValues = null, object? newValues = null, string? ipAddress = null)
    {
        var entry = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            Username = username,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
        _logger.LogDebug("Audit: {Action} on {EntityType}#{EntityId} by {Username}", action, entityType, entityId, username);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(string? entityType, int? entityId, string? username,
        string? fromDate, string? toDate, int page = 1, int pageSize = 20)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrEmpty(username))
            query = query.Where(a => a.Username == username);
        if (DateTime.TryParse(fromDate, out var from))
            query = query.Where(a => a.Timestamp >= from);
        if (DateTime.TryParse(toDate, out var to))
            query = query.Where(a => a.Timestamp <= to.AddDays(1));

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var data = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id, a.Action, a.EntityType, a.EntityId,
                a.UserId, a.Username, a.OldValues, a.NewValues,
                a.IpAddress, a.Timestamp
            ))
            .ToListAsync();

        return new PagedResult<AuditLogDto>(data, new PaginationMeta(page, pageSize, totalCount, totalPages));
    }
}
