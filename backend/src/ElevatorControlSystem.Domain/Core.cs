namespace ElevatorControlSystem.Domain;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public static class DomainConstants
{
    public const decimal MaxLoadKg = 400m;

    public const int SecondsPerFloor = 5;

    public const int DoorOpenSeconds = 2;

    public const int PassengerCallDelaySeconds = 1;

    public const int PassengerRetentionSeconds = 5;
}

public enum UserRole
{
    Admin = 1,
    Operator = 2,
    Viewer = 3
}

public enum SessionStatus
{
    Draft = 1,
    Running = 2,
    Stopped = 3
}

public enum PassengerStatus
{
    Created = 1,
    WaitingElevator = 2,
    Riding = 3,
    Delivered = 4,
    Archived = 5
}

public enum Direction
{
    None = 0,
    Up = 1,
    Down = 2
}

public enum ElevatorMovementState
{
    IdleClosed = 1,
    IdleOpen = 2,
    MovingUp = 3,
    MovingDown = 4
}
