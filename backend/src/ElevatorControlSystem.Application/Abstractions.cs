using Microsoft.EntityFrameworkCore;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> ApplicationUsers { get; }

    DbSet<SimulationSession> SimulationSessions { get; }

    DbSet<ElevatorState> ElevatorStates { get; }

    DbSet<Passenger> Passengers { get; }

    DbSet<FloorCall> FloorCalls { get; }

    DbSet<CabinRequest> CabinRequests { get; }

    DbSet<Trip> Trips { get; }

    DbSet<SessionReport> SessionReports { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUserContext
{
    Guid? UserId { get; }

    string Username { get; }

    UserRole? Role { get; }

    string IpAddress { get; }

    bool IsAuthenticated { get; }
}

public sealed class GeneratedToken
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }
}

public interface IJwtTokenService
{
    GeneratedToken GenerateToken(ApplicationUser user);
}

public interface IPasswordHasherService
{
    string HashPassword(ApplicationUser user, string password);

    bool VerifyPassword(ApplicationUser user, string password);
}

public interface IReportExportService
{
    Task<byte[]> ExportExcelAsync(
        SimulationSession session,
        SessionReport report,
        CancellationToken cancellationToken);
}

public interface ISimulationNotifier
{
    Task BroadcastSnapshotAsync(
        SessionSnapshotDto snapshot,
        CancellationToken cancellationToken);
}

public interface ISessionSnapshotBuilder
{
    SessionSnapshotDto Build(SimulationSession session);
}

public interface IAuditService
{
    Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        string details,
        CancellationToken cancellationToken);

    Task WriteAsync(
        Guid? userId,
        string username,
        string action,
        string entityType,
        string? entityId,
        string details,
        string? ipAddress,
        CancellationToken cancellationToken);
}
