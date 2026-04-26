using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public interface IElevatorStateHandler
{
    ElevatorMovementState State { get; }

    Task HandleAsync(
        SimulationSession session,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}

public sealed class ElevatorStateHandlerFactory
{
    private readonly IReadOnlyDictionary<ElevatorMovementState, IElevatorStateHandler> _handlers;

    public ElevatorStateHandlerFactory(IEnumerable<IElevatorStateHandler> handlers)
    {
        _handlers = handlers.ToDictionary(x => x.State, x => x);
    }

    public IElevatorStateHandler Resolve(ElevatorMovementState state)
    {
        if (_handlers.TryGetValue(state, out var handler))
        {
            return handler;
        }

        throw new ValidationApplicationException($"Обработчик для состояния '{state}' не зарегистрирован.");
    }
}

public sealed class IdleClosedStateHandler : IElevatorStateHandler
{
    private readonly SimulationOperations _simulationOperations;

    public IdleClosedStateHandler(SimulationOperations simulationOperations)
    {
        _simulationOperations = simulationOperations;
    }

    public ElevatorMovementState State => ElevatorMovementState.IdleClosed;

    public Task HandleAsync(
        SimulationSession session,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        _simulationOperations.HandleIdleClosed(session, nowUtc);

        return Task.CompletedTask;
    }
}

public sealed class IdleOpenStateHandler : IElevatorStateHandler
{
    private readonly SimulationOperations _simulationOperations;

    public IdleOpenStateHandler(SimulationOperations simulationOperations)
    {
        _simulationOperations = simulationOperations;
    }

    public ElevatorMovementState State => ElevatorMovementState.IdleOpen;

    public Task HandleAsync(
        SimulationSession session,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        _simulationOperations.HandleIdleOpen(session, nowUtc);

        return Task.CompletedTask;
    }
}

public sealed class MovingUpStateHandler : IElevatorStateHandler
{
    private readonly SimulationOperations _simulationOperations;

    public MovingUpStateHandler(SimulationOperations simulationOperations)
    {
        _simulationOperations = simulationOperations;
    }

    public ElevatorMovementState State => ElevatorMovementState.MovingUp;

    public Task HandleAsync(
        SimulationSession session,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        _simulationOperations.HandleMoving(session, nowUtc, Direction.Up);

        return Task.CompletedTask;
    }
}

public sealed class MovingDownStateHandler : IElevatorStateHandler
{
    private readonly SimulationOperations _simulationOperations;

    public MovingDownStateHandler(SimulationOperations simulationOperations)
    {
        _simulationOperations = simulationOperations;
    }

    public ElevatorMovementState State => ElevatorMovementState.MovingDown;

    public Task HandleAsync(
        SimulationSession session,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        _simulationOperations.HandleMoving(session, nowUtc, Direction.Down);

        return Task.CompletedTask;
    }
}

public sealed class SimulationOperations
{
    private readonly IDestinationSelectionStrategy _destinationSelectionStrategy;
    private readonly IApplicationDbContext _dbContext;

    public SimulationOperations(
        IDestinationSelectionStrategy destinationSelectionStrategy,
        IApplicationDbContext dbContext)
    {
        _destinationSelectionStrategy = destinationSelectionStrategy;
        _dbContext = dbContext;
    }

    public void InitializePassengersOnSessionStart(
        SimulationSession session,
        DateTime nowUtc)
    {
        foreach (var passenger in session.Passengers.Where(x => x.Status == PassengerStatus.Created))
        {
            passenger.CallPressPlannedAtUtc = nowUtc.AddSeconds(
                DomainConstants.PassengerCallDelaySeconds);
        }
    }

    public void RequestGoCommand(
        SimulationSession session,
        DateTime nowUtc)
    {
        session.ElevatorState.GoCommandPending = true;
        session.ElevatorState.LastGoPressedAtUtc = nowUtc;
    }

    public void ProcessPendingPassengerCalls(
        SimulationSession session,
        DateTime nowUtc)
    {
        var pendingPassengers = session.Passengers
            .Where(
                x => x.Status == PassengerStatus.Created
                    && x.CallPressPlannedAtUtc.HasValue
                    && x.CallPressPlannedAtUtc.Value <= nowUtc)
            .ToList();

        foreach (var passenger in pendingPassengers)
        {
            passenger.Status = PassengerStatus.WaitingElevator;
            passenger.CurrentFloor = passenger.SourceFloor;
            passenger.CallPressedAtUtc = nowUtc;

            PressFloorCall(session, passenger.SourceFloor, nowUtc);
        }
    }

    public void ArchiveDeliveredPassengers(
        SimulationSession session,
        DateTime nowUtc)
    {
        var deliveredPassengers = session.Passengers
            .Where(
                x => x.Status == PassengerStatus.Delivered
                    && x.DeliveredAtUtc.HasValue
                    && x.DeliveredAtUtc.Value.AddSeconds(DomainConstants.PassengerRetentionSeconds) <= nowUtc)
            .ToList();

        foreach (var passenger in deliveredPassengers)
        {
            passenger.Status = PassengerStatus.Archived;
            passenger.ArchivedAtUtc = nowUtc;
            passenger.CurrentFloor = passenger.TargetFloor;
        }
    }

    public void UpdateCurrentLoad(SimulationSession session)
    {
        var elevator = session.ElevatorState;

        var ridingPassengers = session.Passengers
            .Where(x => x.Status == PassengerStatus.Riding)
            .ToList();

        elevator.CurrentLoadKg = ridingPassengers.Sum(x => x.WeightKg);
        elevator.PassengerCount = ridingPassengers.Count;

        if (elevator.ActiveTripId.HasValue && ridingPassengers.Count > 0)
        {
            var trip = session.Trips.FirstOrDefault(x => x.Id == elevator.ActiveTripId.Value);

            if (trip is not null)
            {
                trip.WasEmpty = false;
            }
        }
    }

    public void HandleIdleClosed(
        SimulationSession session,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;

        elevator.CurrentPosition = elevator.CurrentFloor;

        if (session.StopRequested && elevator.CurrentLoadKg == 0m)
        {
            FinalizeStop(session, nowUtc);
            return;
        }

        if (!_destinationSelectionStrategy.HasAnyRequests(session))
        {
            EndActiveTrip(session, nowUtc);
            elevator.Direction = Direction.None;
            elevator.GoCommandPending = false;
            return;
        }

        if (CurrentFloorServiceInspector.ShouldAutoServeCurrentFloorBeforeMovement(session))
        {
            OpenDoors(session, nowUtc);
            return;
        }

        var hasActiveTrip = GetActiveTrip(session) is not null;
        var canInitiateMovement = hasActiveTrip || elevator.GoCommandPending;

        if (!canInitiateMovement)
        {
            return;
        }

        if (elevator.Direction != Direction.None
            && _destinationSelectionStrategy.HasRequestsInDirection(
                session,
                elevator.CurrentFloor,
                elevator.Direction))
        {
            StartMovement(session, elevator.Direction, nowUtc);
            return;
        }

        var nextDirection = _destinationSelectionStrategy.SelectDirectionFromIdle(
            session,
            elevator.CurrentFloor);

        if (nextDirection != Direction.None)
        {
            StartMovement(session, nextDirection, nowUtc);
            return;
        }

        if (_destinationSelectionStrategy.ShouldStopAtFloor(session, elevator.CurrentFloor))
        {
            OpenDoors(session, nowUtc);
            return;
        }

        EndActiveTrip(session, nowUtc);
        elevator.Direction = Direction.None;
        elevator.GoCommandPending = false;
    }

    public void HandleIdleOpen(
        SimulationSession session,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;

        if (!elevator.NextActionAtUtc.HasValue)
        {
            elevator.NextActionAtUtc = nowUtc.AddSeconds(DomainConstants.DoorOpenSeconds);
            return;
        }

        if (nowUtc < elevator.NextActionAtUtc.Value)
        {
            return;
        }

        ServeCurrentFloorWhileDoorsOpen(session, nowUtc);

        CloseDoors(session, nowUtc);

        if (session.StopRequested && elevator.CurrentLoadKg == 0m)
        {
            FinalizeStop(session, nowUtc);
            return;
        }

        HandleIdleClosed(session, nowUtc);
    }

    public void HandleMoving(
        SimulationSession session,
        DateTime nowUtc,
        Direction direction)
    {
        var elevator = session.ElevatorState;

        if (!elevator.SegmentStartedAtUtc.HasValue
            || !elevator.SegmentStartFloor.HasValue
            || !elevator.SegmentEndFloor.HasValue)
        {
            StartMovement(session, direction, nowUtc);
            return;
        }

        var segmentDuration = TimeSpan.FromSeconds(DomainConstants.SecondsPerFloor);
        var elapsed = nowUtc - elevator.SegmentStartedAtUtc.Value;
        var progress = Math.Clamp(
            (decimal)(elapsed.TotalMilliseconds / segmentDuration.TotalMilliseconds),
            0m,
            1m);

        elevator.CurrentPosition = direction == Direction.Up
            ? elevator.SegmentStartFloor.Value + progress
            : elevator.SegmentStartFloor.Value - progress;

        if (progress < 1m)
        {
            return;
        }

        elevator.CurrentFloor = elevator.SegmentEndFloor.Value;
        elevator.CurrentPosition = elevator.CurrentFloor;
        elevator.SegmentStartFloor = null;
        elevator.SegmentEndFloor = null;
        elevator.SegmentStartedAtUtc = null;

        UpdateCurrentLoad(session);

        if (session.StopRequested && elevator.CurrentLoadKg == 0m)
        {
            FinalizeStop(session, nowUtc);
            return;
        }

        if (_destinationSelectionStrategy.ShouldStopAtFloor(session, elevator.CurrentFloor))
        {
            OpenDoors(session, nowUtc);
            return;
        }

        ContinueAfterArrival(session, nowUtc);
    }

    public void FinalizeStop(
        SimulationSession session,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;

        CloseDoors(session, nowUtc);
        EndActiveTrip(session, nowUtc);

        session.Status = SessionStatus.Stopped;
        session.StopRequested = false;
        session.StoppedAtUtc = nowUtc;

        elevator.Direction = Direction.None;
        elevator.CurrentPosition = elevator.CurrentFloor;
        elevator.MovementState = ElevatorMovementState.IdleClosed;
        elevator.OverloadIndicatorOn = false;
        elevator.GoCommandPending = false;

        UpdateCurrentLoad(session);

        session.Report ??= new SessionReport
        {
            SimulationSessionId = session.Id
        };

        session.Report.TotalTrips = session.Trips.Count;
        session.Report.EmptyTrips = session.Trips.Count(x => x.WasEmpty);
        session.Report.TotalTransportedWeightKg = session.Passengers
            .Where(
                x => x.Status == PassengerStatus.Delivered
                    || x.Status == PassengerStatus.Archived)
            .Sum(x => x.WeightKg);
        session.Report.TotalCreatedPassengers = session.Passengers.Count;
        session.Report.GeneratedAtUtc = nowUtc;
    }

    private void OpenDoors(
        SimulationSession session,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;
        var currentFloor = elevator.CurrentFloor;

        ClearFloorCall(session, currentFloor, nowUtc);
        ClearCabinRequest(session, currentFloor, nowUtc);
        UnloadPassengers(session, nowUtc);

        UpdateCurrentLoad(session);

        var overloadOccurred = BoardPassengers(session, nowUtc);

        UpdateCurrentLoad(session);

        elevator.MovementState = ElevatorMovementState.IdleOpen;
        elevator.DoorsAreOpen = true;
        elevator.OverloadIndicatorOn = overloadOccurred;
        elevator.CurrentPosition = currentFloor;
        elevator.SegmentStartFloor = null;
        elevator.SegmentEndFloor = null;
        elevator.SegmentStartedAtUtc = null;
        elevator.NextActionAtUtc = nowUtc.AddSeconds(DomainConstants.DoorOpenSeconds);
    }

    private void ServeCurrentFloorWhileDoorsOpen(
        SimulationSession session,
        DateTime nowUtc)
    {
        var currentFloor = session.ElevatorState.CurrentFloor;

        ClearFloorCall(session, currentFloor, nowUtc);

        var overloadOccurred = BoardPassengers(session, nowUtc);

        UpdateCurrentLoad(session);

        if (overloadOccurred)
        {
            session.ElevatorState.OverloadIndicatorOn = true;
        }
    }

    private void CloseDoors(
        SimulationSession session,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;

        elevator.MovementState = ElevatorMovementState.IdleClosed;
        elevator.DoorsAreOpen = false;
        elevator.OverloadIndicatorOn = false;
        elevator.CurrentPosition = elevator.CurrentFloor;
        elevator.NextActionAtUtc = null;
        elevator.SegmentStartFloor = null;
        elevator.SegmentEndFloor = null;
        elevator.SegmentStartedAtUtc = null;
        elevator.LastServicedFloor = elevator.CurrentFloor;
        elevator.LastServicedAtUtc = nowUtc;
    }

    private void StartMovement(
        SimulationSession session,
        Direction direction,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;

        if (direction == Direction.None)
        {
            return;
        }

        if (elevator.CurrentLoadKg > DomainConstants.MaxLoadKg)
        {
            elevator.OverloadIndicatorOn = true;
            return;
        }

        if (direction == Direction.Up && elevator.CurrentFloor >= session.FloorCount)
        {
            ContinueAfterArrival(session, nowUtc);
            return;
        }

        if (direction == Direction.Down && elevator.CurrentFloor <= 1)
        {
            ContinueAfterArrival(session, nowUtc);
            return;
        }

        var activeTrip = GetActiveTrip(session);

        if (activeTrip is null || activeTrip.Direction != direction)
        {
            if (activeTrip is not null)
            {
                EndActiveTrip(session, nowUtc);
            }

            StartTrip(session, direction, nowUtc);
        }

        elevator.Direction = direction;
        elevator.MovementState = direction == Direction.Up
            ? ElevatorMovementState.MovingUp
            : ElevatorMovementState.MovingDown;
        elevator.DoorsAreOpen = false;
        elevator.OverloadIndicatorOn = false;
        elevator.CurrentPosition = elevator.CurrentFloor;
        elevator.NextActionAtUtc = null;
        elevator.SegmentStartFloor = elevator.CurrentFloor;
        elevator.SegmentEndFloor = direction == Direction.Up
            ? elevator.CurrentFloor + 1
            : elevator.CurrentFloor - 1;
        elevator.SegmentStartedAtUtc = nowUtc;
        elevator.GoCommandPending = false;
        elevator.LastServicedFloor = null;
        elevator.LastServicedAtUtc = null;
    }

    private void ContinueAfterArrival(
        SimulationSession session,
        DateTime nowUtc)
    {
        var elevator = session.ElevatorState;

        if (elevator.Direction != Direction.None
            && _destinationSelectionStrategy.HasRequestsInDirection(
                session,
                elevator.CurrentFloor,
                elevator.Direction))
        {
            StartMovement(session, elevator.Direction, nowUtc);
            return;
        }

        if (_destinationSelectionStrategy.HasAnyRequests(session))
        {
            var nextDirection = _destinationSelectionStrategy.SelectDirectionFromIdle(
                session,
                elevator.CurrentFloor);

            if (nextDirection != Direction.None)
            {
                StartMovement(session, nextDirection, nowUtc);
                return;
            }
        }

        EndActiveTrip(session, nowUtc);
        elevator.Direction = Direction.None;
        elevator.MovementState = ElevatorMovementState.IdleClosed;
        elevator.CurrentPosition = elevator.CurrentFloor;
        elevator.GoCommandPending = false;
    }

    private void UnloadPassengers(
        SimulationSession session,
        DateTime nowUtc)
    {
        var currentFloor = session.ElevatorState.CurrentFloor;

        var passengersForExit = session.Passengers
            .Where(
                x => x.Status == PassengerStatus.Riding
                    && x.TargetFloor == currentFloor)
            .ToList();

        foreach (var passenger in passengersForExit)
        {
            passenger.Status = PassengerStatus.Delivered;
            passenger.DeliveredAtUtc = nowUtc;
            passenger.CurrentFloor = currentFloor;
        }
    }

    private bool BoardPassengers(
        SimulationSession session,
        DateTime nowUtc)
    {
        var overloadOccurred = false;
        var currentFloor = session.ElevatorState.CurrentFloor;

        var waitingPassengers = session.Passengers
            .Where(
                x => x.Status == PassengerStatus.WaitingElevator
                    && x.SourceFloor == currentFloor)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        foreach (var passenger in waitingPassengers)
        {
            var projectedLoad = session.ElevatorState.CurrentLoadKg + passenger.WeightKg;

            if (projectedLoad > DomainConstants.MaxLoadKg)
            {
                overloadOccurred = true;
                continue;
            }

            passenger.Status = PassengerStatus.Riding;
            passenger.BoardedAtUtc ??= nowUtc;
            passenger.CurrentFloor = currentFloor;

            PressCabinRequest(session, passenger.TargetFloor, nowUtc);

            session.ElevatorState.CurrentLoadKg = projectedLoad;
            session.ElevatorState.PassengerCount += 1;
        }

        var waitingPassengersRemain = session.Passengers.Any(
            x => x.Status == PassengerStatus.WaitingElevator
                && x.SourceFloor == currentFloor);

        if (waitingPassengersRemain)
        {
            PressFloorCall(session, currentFloor, nowUtc);
        }

        return overloadOccurred;
    }

    private void PressFloorCall(
        SimulationSession session,
        int floorNumber,
        DateTime nowUtc)
    {
        var floorCall = session.GetFloorCall(floorNumber);

        floorCall.IsPressed = true;
        floorCall.PressedAtUtc = nowUtc;
        floorCall.ClearedAtUtc = null;
    }

    private void ClearFloorCall(
        SimulationSession session,
        int floorNumber,
        DateTime nowUtc)
    {
        var floorCall = session.GetFloorCall(floorNumber);

        floorCall.IsPressed = false;
        floorCall.ClearedAtUtc = nowUtc;
    }

    private void PressCabinRequest(
        SimulationSession session,
        int floorNumber,
        DateTime nowUtc)
    {
        var cabinRequest = session.GetCabinRequest(floorNumber);

        cabinRequest.IsPressed = true;
        cabinRequest.PressedAtUtc = nowUtc;
        cabinRequest.ClearedAtUtc = null;
    }

    private void ClearCabinRequest(
        SimulationSession session,
        int floorNumber,
        DateTime nowUtc)
    {
        var cabinRequest = session.GetCabinRequest(floorNumber);

        cabinRequest.IsPressed = false;
        cabinRequest.ClearedAtUtc = nowUtc;
    }

    private Trip? GetActiveTrip(SimulationSession session)
    {
        var activeTripId = session.ElevatorState.ActiveTripId;

        if (!activeTripId.HasValue)
        {
            return null;
        }

        var trip = session.Trips.FirstOrDefault(x => x.Id == activeTripId.Value);

        if (trip is null)
        {
            session.ElevatorState.ActiveTripId = null;
            return null;
        }

        return trip;
    }

    private void StartTrip(
        SimulationSession session,
        Direction direction,
        DateTime nowUtc)
    {
        // Новую поездку нужно явно добавлять в DbContext,
        // иначе EF в этом сценарии периодически пытается выполнить UPDATE
        // вместо INSERT и выбрасывает DbUpdateConcurrencyException.
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            SimulationSessionId = session.Id,
            SimulationSession = session,
            SequenceNumber = session.Trips.Count + 1,
            Direction = direction,
            StartedFloor = session.ElevatorState.CurrentFloor,
            StartedAtUtc = nowUtc,
            WasEmpty = session.ElevatorState.CurrentLoadKg == 0m
        };

        _dbContext.Trips.Add(trip);

        session.ElevatorState.ActiveTripId = trip.Id;
    }

    private void EndActiveTrip(
        SimulationSession session,
        DateTime nowUtc)
    {
        var activeTrip = GetActiveTrip(session);

        if (activeTrip is null)
        {
            session.ElevatorState.ActiveTripId = null;
            return;
        }

        activeTrip.EndedAtUtc ??= nowUtc;
        activeTrip.EndedFloor ??= session.ElevatorState.CurrentFloor;
        session.ElevatorState.ActiveTripId = null;
    }
}

public sealed class SimulationRuntimeService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IClock _clock;
    private readonly SimulationOperations _simulationOperations;
    private readonly ElevatorStateHandlerFactory _stateHandlerFactory;
    private readonly ISessionSnapshotBuilder _snapshotBuilder;
    private readonly ISimulationNotifier _simulationNotifier;
    private readonly ILogger<SimulationRuntimeService> _logger;

    public SimulationRuntimeService(
        IApplicationDbContext dbContext,
        IClock clock,
        SimulationOperations simulationOperations,
        ElevatorStateHandlerFactory stateHandlerFactory,
        ISessionSnapshotBuilder snapshotBuilder,
        ISimulationNotifier simulationNotifier,
        ILogger<SimulationRuntimeService> logger)
    {
        _dbContext = dbContext;
        _clock = clock;
        _simulationOperations = simulationOperations;
        _stateHandlerFactory = stateHandlerFactory;
        _snapshotBuilder = snapshotBuilder;
        _simulationNotifier = simulationNotifier;
        _logger = logger;
    }

    public async Task ProcessRunningSessionsAsync(CancellationToken cancellationToken)
    {
        var sessionIds = await _dbContext.SimulationSessions
            .AsNoTracking()
            .Where(x => x.Status == SessionStatus.Running)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var sessionId in sessionIds)
        {
            try
            {
                var session = await _dbContext.SimulationSessions
                    .IncludeAggregate()
                    .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

                if (session is null)
                {
                    continue;
                }

                var nowUtc = _clock.UtcNow;

                _simulationOperations.ProcessPendingPassengerCalls(session, nowUtc);
                _simulationOperations.ArchiveDeliveredPassengers(session, nowUtc);
                _simulationOperations.UpdateCurrentLoad(session);

                var handler = _stateHandlerFactory.Resolve(session.ElevatorState.MovementState);

                await handler.HandleAsync(session, nowUtc, cancellationToken);

                _simulationOperations.UpdateCurrentLoad(session);

                await _dbContext.SaveChangesAsync(cancellationToken);

                var snapshot = _snapshotBuilder.Build(session);

                await _simulationNotifier.BroadcastSnapshotAsync(snapshot, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Ошибка при обработке сеанса симуляции {SessionId}.",
                    sessionId);
            }
        }
    }
}
