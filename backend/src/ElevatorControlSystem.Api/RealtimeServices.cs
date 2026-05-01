using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ElevatorControlSystem.Application;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Api;

[Authorize]
public sealed class SimulationHub : Hub
{
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}

public sealed class SignalRSimulationNotifier : ISimulationNotifier
{
    private readonly IHubContext<SimulationHub> _hubContext;

    public SignalRSimulationNotifier(IHubContext<SimulationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastSnapshotAsync(
        SessionSnapshotDto snapshot,
        CancellationToken cancellationToken)
    {
        await _hubContext.Clients
            .Group(snapshot.SessionId.ToString())
            .SendAsync("snapshotUpdated", snapshot, cancellationToken);
    }
}

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var rawValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(rawValue, out var userId)
                ? userId
                : null;
        }
    }

    public string Username
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        }
    }

    public UserRole? Role
    {
        get
        {
            var rawValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

            return Enum.TryParse<UserRole>(rawValue, out var role)
                ? role
                : null;
        }
    }

    public string IpAddress
    {
        get
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
                ?? string.Empty;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
        }
    }
}
