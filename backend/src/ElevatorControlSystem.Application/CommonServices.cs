using Microsoft.EntityFrameworkCore;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public sealed class AuditService : IAuditService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AuditService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId,
        string details,
        CancellationToken cancellationToken)
    {
        await WriteAsync(
            _currentUserContext.UserId,
            _currentUserContext.Username,
            action,
            entityType,
            entityId,
            details,
            _currentUserContext.IpAddress,
            cancellationToken);
    }

    public async Task WriteAsync(
        Guid? userId,
        string username,
        string action,
        string entityType,
        string? entityId,
        string details,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Username = string.IsNullOrWhiteSpace(username) ? "system" : username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress ?? string.Empty
        };

        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal static class CurrentFloorServiceInspector
{
    public static bool HasRequestsOutsideCurrentFloor(
        SimulationSession session,
        int currentFloor)
    {
        var hasFloorCallsOutside = session.FloorCalls.Any(
            x => x.IsPressed && x.FloorNumber != currentFloor);

        var hasCabinRequestsOutside = session.CabinRequests.Any(
            x => x.IsPressed && x.FloorNumber != currentFloor);

        return hasFloorCallsOutside || hasCabinRequestsOutside;
    }

    public static bool HasCurrentFloorStopReason(SimulationSession session)
    {
        var currentFloor = session.ElevatorState.CurrentFloor;

        var hasFloorCall = session.FloorCalls.Any(
            x => x.IsPressed && x.FloorNumber == currentFloor);

        var hasCabinRequest = session.CabinRequests.Any(
            x => x.IsPressed && x.FloorNumber == currentFloor);

        var hasPassengerForExit = session.Passengers.Any(
            x => x.Status == PassengerStatus.Riding && x.TargetFloor == currentFloor);

        return hasFloorCall || hasCabinRequest || hasPassengerForExit;
    }

    public static bool HasFreshCurrentFloorStopReasonSinceLastService(
        SimulationSession session)
    {
        var currentFloor = session.ElevatorState.CurrentFloor;
        var lastServiceAtUtc = GetLastServiceAtUtcForCurrentFloor(session);

        var hasPassengerForExit = session.Passengers.Any(
            x => x.Status == PassengerStatus.Riding && x.TargetFloor == currentFloor);

        if (hasPassengerForExit)
        {
            return true;
        }

        var hasFreshFloorCall = session.FloorCalls.Any(
            x => x.FloorNumber == currentFloor
                && x.IsPressed
                && IsPressedAfterLastService(x.PressedAtUtc, lastServiceAtUtc));

        var hasFreshCabinRequest = session.CabinRequests.Any(
            x => x.FloorNumber == currentFloor
                && x.IsPressed
                && IsPressedAfterLastService(x.PressedAtUtc, lastServiceAtUtc));

        return hasFreshFloorCall || hasFreshCabinRequest;
    }

    public static bool ShouldAutoServeCurrentFloorBeforeMovement(
        SimulationSession session)
    {
        var currentFloor = session.ElevatorState.CurrentFloor;

        if (!HasCurrentFloorStopReason(session))
        {
            return false;
        }

        if (!HasRequestsOutsideCurrentFloor(session, currentFloor))
        {
            return true;
        }

        // Недостаточно знать только номер этажа.
        // Нужно понимать, была ли текущая заявка нажата уже после последнего
        // обслуживания этого этажа. Иначе лифт снова откроет двери сразу после
        // закрытия и зациклится на одном месте.
        return HasFreshCurrentFloorStopReasonSinceLastService(session);
    }

    private static DateTime? GetLastServiceAtUtcForCurrentFloor(
        SimulationSession session)
    {
        var elevator = session.ElevatorState;

        if (elevator.LastServicedFloor != elevator.CurrentFloor)
        {
            return null;
        }

        return elevator.LastServicedAtUtc;
    }

    private static bool IsPressedAfterLastService(
        DateTime? pressedAtUtc,
        DateTime? lastServiceAtUtc)
    {
        if (!lastServiceAtUtc.HasValue)
        {
            return true;
        }

        if (!pressedAtUtc.HasValue)
        {
            return true;
        }

        return pressedAtUtc.Value > lastServiceAtUtc.Value;
    }
}

internal static class PassengerPresentationMapper
{
    public static PassengerDto Map(
        SimulationSession session,
        Passenger passenger)
    {
        var currentFloor = passenger.Status == PassengerStatus.Riding
            ? Math.Clamp(
                (int)Math.Round(
                    (double)session.ElevatorState.CurrentPosition,
                    MidpointRounding.AwayFromZero),
                1,
                session.FloorCount)
            : passenger.CurrentFloor;

        var locationDescription = passenger.Status switch
        {
            PassengerStatus.Created => $"ожидает лифта на этаже {passenger.SourceFloor}",
            PassengerStatus.WaitingElevator => $"ожидает лифта на этаже {passenger.SourceFloor}",
            PassengerStatus.Riding when session.ElevatorState.MovementState is ElevatorMovementState.MovingUp or ElevatorMovementState.MovingDown
                => $"находится в движущемся лифте на уровне этажа {currentFloor}",
            PassengerStatus.Riding => $"находится в лифте на этаже {currentFloor}",
            PassengerStatus.Delivered => $"доставлен на целевой этаж {passenger.TargetFloor}",
            PassengerStatus.Archived => $"доставлен на целевой этаж {passenger.TargetFloor}",
            _ => $"ожидает лифта на этаже {passenger.SourceFloor}"
        };

        var statusDescription = passenger.Status switch
        {
            PassengerStatus.Created => "создан, ожидает автоматического вызова",
            PassengerStatus.WaitingElevator => "ожидает лифт",
            PassengerStatus.Riding => "находится в лифте",
            PassengerStatus.Delivered => "доставлен на целевой этаж",
            PassengerStatus.Archived => "завершил участие в сеансе",
            _ => "неизвестный статус"
        };

        return new PassengerDto
        {
            Id = passenger.Id,
            WeightKg = passenger.WeightKg,
            SourceFloor = passenger.SourceFloor,
            TargetFloor = passenger.TargetFloor,
            CurrentFloor = currentFloor,
            Status = passenger.Status,
            StatusDescription = statusDescription,
            LocationDescription = locationDescription,
            IsInElevator = passenger.Status == PassengerStatus.Riding,
            CreatedAtUtc = passenger.CreatedAtUtc,
            CallPressedAtUtc = passenger.CallPressedAtUtc,
            BoardedAtUtc = passenger.BoardedAtUtc,
            DeliveredAtUtc = passenger.DeliveredAtUtc
        };
    }
}

public sealed class SessionSnapshotBuilder : ISessionSnapshotBuilder
{
    private readonly IClock _clock;

    public SessionSnapshotBuilder(IClock clock)
    {
        _clock = clock;
    }

    public SessionSnapshotDto Build(SimulationSession session)
    {
        var elevator = session.ElevatorState;
        var isMoving = elevator.MovementState is ElevatorMovementState.MovingUp
            or ElevatorMovementState.MovingDown;

        var hasRequestsOutsideCurrentFloor = CurrentFloorServiceInspector.HasRequestsOutsideCurrentFloor(
            session,
            elevator.CurrentFloor);

        var awaitingGoCommand = session.Status == SessionStatus.Running
            && hasRequestsOutsideCurrentFloor
            && !elevator.GoCommandPending
            && !elevator.ActiveTripId.HasValue
            && (elevator.MovementState is ElevatorMovementState.IdleClosed
                or ElevatorMovementState.IdleOpen)
            && !CurrentFloorServiceInspector.ShouldAutoServeCurrentFloorBeforeMovement(session);

        return new SessionSnapshotDto
        {
            SessionId = session.Id,
            SessionName = session.Name,
            Status = session.Status,
            StopRequested = session.StopRequested,
            FloorCount = session.FloorCount,
            Elevator = new ElevatorStateDto
            {
                CurrentFloor = elevator.CurrentFloor,
                CurrentPosition = elevator.CurrentPosition,
                MovementState = elevator.MovementState,
                Direction = elevator.Direction,
                DoorsAreOpen = elevator.DoorsAreOpen,
                OverloadIndicatorOn = elevator.OverloadIndicatorOn,
                CurrentLoadKg = elevator.CurrentLoadKg,
                MaxLoadKg = DomainConstants.MaxLoadKg,
                PassengerCount = elevator.PassengerCount,
                GoCommandPending = elevator.GoCommandPending,
                AwaitingGoCommand = awaitingGoCommand
            },
            FloorCalls = session.FloorCalls
                .OrderBy(x => x.FloorNumber)
                .Select(x => new ButtonStateDto
                {
                    FloorNumber = x.FloorNumber,
                    IsPressed = x.IsPressed
                })
                .ToList(),
            CabinRequests = session.CabinRequests
                .OrderBy(x => x.FloorNumber)
                .Select(x => new ButtonStateDto
                {
                    FloorNumber = x.FloorNumber,
                    IsPressed = x.IsPressed
                })
                .ToList(),
            Passengers = session.Passengers
                .Where(x => x.Status != PassengerStatus.Archived)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => PassengerPresentationMapper.Map(session, x))
                .ToList(),
            StatusBar = new StatusBarDto
            {
                MovingElevatorsCount = isMoving ? 1 : 0,
                StoppedElevatorsCount = isMoving ? 0 : 1,
                TransportedPassengersCount = session.Passengers.Count(x => x.BoardedAtUtc.HasValue)
            },
            ServerTimeUtc = _clock.UtcNow
        };
    }
}

public static class SimulationSessionQueryExtensions
{
    public static IQueryable<SimulationSession> IncludeAggregate(
        this IQueryable<SimulationSession> query)
    {
        return query
            .Include(x => x.ElevatorState)
            .Include(x => x.Passengers)
            .Include(x => x.FloorCalls)
            .Include(x => x.CabinRequests)
            .Include(x => x.Trips)
            .Include(x => x.Report);
    }
}
