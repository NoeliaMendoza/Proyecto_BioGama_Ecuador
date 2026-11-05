using BioGamaEcuador.Data;
using BioGamaEcuador.Models;
using Microsoft.Extensions.Logging;

namespace BioGamaEcuador.Services;

public class AuditService : IAuditService
{
    private readonly ILogger<AuditService> _logger;
    private readonly AppDbContext _db;

    public AuditService(ILogger<AuditService> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task LogAsync(string action, string entity, string? entityId, string? oldValue, string? newValue, string userId, string? ipAddress = null)
    {
        BioGamaEcuador.Services.LogEvents.AuditEvent(_logger,action, entity, entityId, userId, ipAddress ?? "-", oldValue ?? "-", newValue ?? "-");

        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IpAddress = ipAddress,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue
        });
        await _db.SaveChangesAsync();
    }

    public async Task LogLoginAsync(string userId, bool success, string? ipAddress = null)
    {
        BioGamaEcuador.Services.LogEvents.AuditLogin(_logger,userId, success, ipAddress ?? "-");

        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IpAddress = ipAddress,
            Action = success ? "LoginSuccess" : "LoginFailed",
            Entity = "User",
            EntityId = userId,
            NewValue = success ? "Success" : "Failed"
        });
        await _db.SaveChangesAsync();
    }

    public async Task LogMfaChangeAsync(string userId, bool enabled, string? ipAddress = null)
    {
        BioGamaEcuador.Services.LogEvents.AuditMfaChange(_logger,userId, enabled, ipAddress ?? "-");

        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            IpAddress = ipAddress,
            Action = enabled ? "MfaEnabled" : "MfaDisabled",
            Entity = "User",
            EntityId = userId,
            OldValue = enabled ? "Disabled" : "Enabled",
            NewValue = enabled ? "Enabled" : "Disabled"
        });
        await _db.SaveChangesAsync();
    }

    public async Task LogRoleChangeAsync(string userId, string oldRole, string newRole, string changedBy)
    {
        BioGamaEcuador.Services.LogEvents.AuditRoleChange(_logger,userId, oldRole, newRole, changedBy);

        _db.AuditLogs.Add(new AuditLog
        {
            CreatedAt = DateTime.UtcNow,
            UserId = changedBy,
            IpAddress = null,
            Action = string.IsNullOrEmpty(oldRole) ? "RoleAdded" : string.IsNullOrEmpty(newRole) ? "RoleRemoved" : "RoleChanged",
            Entity = "User",
            EntityId = userId,
            OldValue = oldRole,
            NewValue = newRole
        });
        await _db.SaveChangesAsync();
    }
}
