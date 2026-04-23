using System.ComponentModel.DataAnnotations;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "Введите имя пользователя.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно содержать от 3 до 50 символов.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать не менее 8 символов.")]
    public string Password { get; set; } = string.Empty;
}

public sealed class CurrentUserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public CurrentUserDto User { get; set; } = new();
}

public sealed class CreatePassengerRequestDto
{
    [Range(20, 400, ErrorMessage = "Вес пассажира должен быть в диапазоне от 20 до 400 кг.")]
    public decimal WeightKg { get; set; }

    [Range(1, 50, ErrorMessage = "Этаж появления должен быть в диапазоне от 1 до 50.")]
    public int SourceFloor { get; set; }

    [Range(1, 50, ErrorMessage = "Целевой этаж должен быть в диапазоне от 1 до 50.")]
    public int TargetFloor { get; set; }
}

public sealed class UpdatePassengerRequestDto
{
    [Range(20, 400, ErrorMessage = "Вес пассажира должен быть в диапазоне от 20 до 400 кг.")]
    public decimal WeightKg { get; set; }

    [Range(1, 50, ErrorMessage = "Этаж появления должен быть в диапазоне от 1 до 50.")]
    public int SourceFloor { get; set; }

    [Range(1, 50, ErrorMessage = "Целевой этаж должен быть в диапазоне от 1 до 50.")]
    public int TargetFloor { get; set; }
}

public sealed class CreateSessionRequestDto
{
    [StringLength(120, ErrorMessage = "Название сеанса не должно превышать 120 символов.")]
    public string? Name { get; set; }

    [Range(2, 50, ErrorMessage = "Количество этажей должно быть в диапазоне от 2 до 50.")]
    public int FloorCount { get; set; }

    public List<CreatePassengerRequestDto> InitialPassengers { get; set; } = [];
}

public sealed class SessionListItemDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int FloorCount { get; set; }

    public SessionStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? StoppedAtUtc { get; set; }

    public int TotalPassengers { get; set; }

    public bool HasReport { get; set; }
}

public sealed class ElevatorStateDto
{
    public int CurrentFloor { get; set; }

    public decimal CurrentPosition { get; set; }

    public ElevatorMovementState MovementState { get; set; }

    public Direction Direction { get; set; }

    public bool DoorsAreOpen { get; set; }

    public bool OverloadIndicatorOn { get; set; }

    public decimal CurrentLoadKg { get; set; }

    public decimal MaxLoadKg { get; set; }

    public int PassengerCount { get; set; }

    public bool GoCommandPending { get; set; }

    public bool AwaitingGoCommand { get; set; }
}

public sealed class ButtonStateDto
{
    public int FloorNumber { get; set; }

    public bool IsPressed { get; set; }
}

public sealed class PassengerDto
{
    public Guid Id { get; set; }

    public decimal WeightKg { get; set; }

    public int SourceFloor { get; set; }

    public int TargetFloor { get; set; }

    public int CurrentFloor { get; set; }

    public PassengerStatus Status { get; set; }

    public string StatusDescription { get; set; } = string.Empty;

    public string LocationDescription { get; set; } = string.Empty;

    public bool IsInElevator { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CallPressedAtUtc { get; set; }

    public DateTime? BoardedAtUtc { get; set; }

    public DateTime? DeliveredAtUtc { get; set; }
}

public sealed class StatusBarDto
{
    public int MovingElevatorsCount { get; set; }

    public int StoppedElevatorsCount { get; set; }

    public int TransportedPassengersCount { get; set; }
}

public sealed class SessionSnapshotDto
{
    public Guid SessionId { get; set; }

    public string SessionName { get; set; } = string.Empty;

    public SessionStatus Status { get; set; }

    public bool StopRequested { get; set; }

    public int FloorCount { get; set; }

    public ElevatorStateDto Elevator { get; set; } = new();

    public IReadOnlyList<ButtonStateDto> FloorCalls { get; set; } = [];

    public IReadOnlyList<ButtonStateDto> CabinRequests { get; set; } = [];

    public IReadOnlyList<PassengerDto> Passengers { get; set; } = [];

    public StatusBarDto StatusBar { get; set; } = new();

    public DateTime ServerTimeUtc { get; set; }
}

public sealed class PassengerLocationDto
{
    public Guid PassengerId { get; set; }

    public string LocationDescription { get; set; } = string.Empty;

    public PassengerStatus Status { get; set; }

    public int CurrentFloor { get; set; }
}

public sealed class SessionReportDto
{
    public Guid SessionId { get; set; }

    public string SessionName { get; set; } = string.Empty;

    public int TotalTrips { get; set; }

    public int EmptyTrips { get; set; }

    public decimal TotalTransportedWeightKg { get; set; }

    public int TotalCreatedPassengers { get; set; }

    public DateTime? SessionStartedAtUtc { get; set; }

    public DateTime? SessionStoppedAtUtc { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public IReadOnlyList<PassengerDto> Passengers { get; set; } = [];
}

public sealed class CreateUserRequestDto
{
    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 50 символов.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите отображаемое имя.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Отображаемое имя должно содержать от 2 до 100 символов.")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать не менее 8 символов.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите роль.")]
    public UserRole Role { get; set; }
}

public sealed class UpdateUserRequestDto
{
    [Required(ErrorMessage = "Введите логин.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 50 символов.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите отображаемое имя.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Отображаемое имя должно содержать от 2 до 100 символов.")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Новый пароль должен содержать не менее 8 символов.")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Укажите роль.")]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; }
}

public sealed class UserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class AuditLogDto
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Details { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AuditLogQueryDto
{
    public string? Username { get; set; }

    public string? Action { get; set; }

    public string? EntityType { get; set; }

    public Guid? SessionId { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public int Limit { get; set; } = 200;
}
