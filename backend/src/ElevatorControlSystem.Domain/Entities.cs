namespace ElevatorControlSystem.Domain;

public sealed class ApplicationUser : AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAtUtc { get; set; }

    public List<SimulationSession> CreatedSessions { get; set; } = [];
}

public sealed class SimulationSession : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public int FloorCount { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Draft;

    public bool StopRequested { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? StoppedAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public ElevatorState ElevatorState { get; set; } = new();

    public List<Passenger> Passengers { get; set; } = [];

    public List<FloorCall> FloorCalls { get; set; } = [];

    public List<CabinRequest> CabinRequests { get; set; } = [];

    public List<Trip> Trips { get; set; } = [];

    public SessionReport? Report { get; set; }

    public FloorCall GetFloorCall(int floorNumber)
    {
        return FloorCalls.Single(x => x.FloorNumber == floorNumber);
    }

    public CabinRequest GetCabinRequest(int floorNumber)
    {
        return CabinRequests.Single(x => x.FloorNumber == floorNumber);
    }
}

public sealed class ElevatorState : AuditableEntity
{
    public Guid SimulationSessionId { get; set; }

    public SimulationSession? SimulationSession { get; set; }

    public int CurrentFloor { get; set; } = 1;

    public decimal CurrentPosition { get; set; } = 1m;

    public ElevatorMovementState MovementState { get; set; } = ElevatorMovementState.IdleClosed;

    public Direction Direction { get; set; } = Direction.None;

    public bool DoorsAreOpen { get; set; }

    public bool OverloadIndicatorOn { get; set; }

    public decimal CurrentLoadKg { get; set; }

    public int PassengerCount { get; set; }

    public int? SegmentStartFloor { get; set; }

    public int? SegmentEndFloor { get; set; }

    public DateTime? SegmentStartedAtUtc { get; set; }

    public DateTime? NextActionAtUtc { get; set; }

    public bool GoCommandPending { get; set; }

    public DateTime? LastGoPressedAtUtc { get; set; }

    public int? LastServicedFloor { get; set; }

    public DateTime? LastServicedAtUtc { get; set; }

    public Guid? ActiveTripId { get; set; }
}

public sealed class Passenger : AuditableEntity
{
    public Guid SimulationSessionId { get; set; }

    public SimulationSession? SimulationSession { get; set; }

    public decimal WeightKg { get; set; }

    public int SourceFloor { get; set; }

    public int TargetFloor { get; set; }

    public PassengerStatus Status { get; set; } = PassengerStatus.Created;

    public int CurrentFloor { get; set; }

    public DateTime? CallPressPlannedAtUtc { get; set; }

    public DateTime? CallPressedAtUtc { get; set; }

    public DateTime? BoardedAtUtc { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class FloorCall : AuditableEntity
{
    public Guid SimulationSessionId { get; set; }

    public SimulationSession? SimulationSession { get; set; }

    public int FloorNumber { get; set; }

    public bool IsPressed { get; set; }

    public DateTime? PressedAtUtc { get; set; }

    public DateTime? ClearedAtUtc { get; set; }
}

public sealed class CabinRequest : AuditableEntity
{
    public Guid SimulationSessionId { get; set; }

    public SimulationSession? SimulationSession { get; set; }

    public int FloorNumber { get; set; }

    public bool IsPressed { get; set; }

    public DateTime? PressedAtUtc { get; set; }

    public DateTime? ClearedAtUtc { get; set; }
}

public sealed class Trip : AuditableEntity
{
    public Guid SimulationSessionId { get; set; }

    public SimulationSession? SimulationSession { get; set; }

    public int SequenceNumber { get; set; }

    public Direction Direction { get; set; }

    public int StartedFloor { get; set; }

    public int? EndedFloor { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    public bool WasEmpty { get; set; } = true;
}

public sealed class SessionReport : AuditableEntity
{
    public Guid SimulationSessionId { get; set; }

    public SimulationSession? SimulationSession { get; set; }

    public int TotalTrips { get; set; }

    public int EmptyTrips { get; set; }

    public decimal TotalTransportedWeightKg { get; set; }

    public int TotalCreatedPassengers { get; set; }

    public DateTime GeneratedAtUtc { get; set; }
}

public sealed class AuditLog : AuditableEntity
{
    public Guid? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Details { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;
}
