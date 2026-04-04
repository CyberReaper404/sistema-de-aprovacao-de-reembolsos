using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Reembolso.Application.Abstractions;
using Reembolso.Domain.Entities;
using Reembolso.Domain.Enums;
using Reembolso.Infrastructure.Persistence;

namespace Reembolso.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditService(
        AppDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IHttpContextAccessor httpContextAccessor,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _httpContextAccessor = httpContextAccessor;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task WriteAsync(
        string eventType,
        string entityType,
        string? entityId,
        AuditSeverity severity,
        object? metadata,
        CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var entry = new AuditEntry(
            eventType,
            entityType,
            entityId,
            _currentUserContext.UserId,
            context?.Connection.RemoteIpAddress?.ToString(),
            context?.Request.Headers.UserAgent.ToString(),
            metadata is null ? null : JsonSerializer.Serialize(metadata),
            severity,
            _dateTimeProvider.UtcNow);

        _dbContext.AuditEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

