using Microsoft.EntityFrameworkCore;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public sealed class SessionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditService _auditService;
    private readonly ISessionSnapshotBuilder _snapshotBuilder;
    private readonly ISimulationNotifier _simulationNotifier;
    private readonly SimulationOperations _simulationOperations;
    private readonly IClock _clock;

    public SessionService(
        IApplicationDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditService auditService,
        ISessionSnapshotBuilder snapshotBuilder,
        ISimulationNotifier simulationNotifier,
        SimulationOperations simulationOperations,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditService = auditService;
        _snapshotBuilder = snapshotBuilder;
        _simulationNotifier = simulationNotifier;
        _simulationOperations = simulationOperations;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SessionListItemDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.SimulationSessions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new SessionListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                FloorCount = x.FloorCount,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                StartedAtUtc = x.StartedAtUtc,
                StoppedAtUtc = x.StoppedAtUtc,
                TotalPassengers = x.Passengers.Count(),
                HasReport = x.Report != null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SessionSnapshotDto> GetCurrentAsync(
        CancellationToken cancellationToken)
    {
        var runningSession = await _dbContext.SimulationSessions
            .IncludeAggregate()
            .FirstOrDefaultAsync(
                x => x.Status == SessionStatus.Running,
                cancellationToken);

        if (runningSession is not null)
        {
            return _snapshotBuilder.Build(runningSession);
        }

        var draftSession = await _dbContext.SimulationSessions
            .IncludeAggregate()
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(
                x => x.Status == SessionStatus.Draft,
                cancellationToken);

        if (draftSession is not null)
        {
            return _snapshotBuilder.Build(draftSession);
        }

        throw new NotFoundApplicationException("Текущий активный или черновой сеанс не найден.");
    }

    public async Task<SessionSnapshotDto> GetSnapshotAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        return _snapshotBuilder.Build(session);
    }

    public async Task<SessionSnapshotDto> CreateAsync(
        CreateSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedApplicationException("Пользователь не авторизован.");
        }

        ValidatePassengerRequests(request.InitialPassengers, request.FloorCount);

        var sessionName = string.IsNullOrWhiteSpace(request.Name)
            ? $"Сеанс {_clock.UtcNow:yyyy-MM-dd HH:mm:ss}"
            : request.Name.Trim();

        var session = new SimulationSession
        {
            Name = sessionName,
            FloorCount = request.FloorCount,
            CreatedByUserId = _currentUserContext.UserId.Value,
            Status = SessionStatus.Draft,
            StopRequested = false
        };

        session.ElevatorState = new ElevatorState
        {
            SimulationSessionId = session.Id,
            CurrentFloor = 1,
            CurrentPosition = 1m,
            MovementState = ElevatorMovementState.IdleClosed,
            Direction = Direction.None,
            DoorsAreOpen = false,
            OverloadIndicatorOn = false,
            GoCommandPending = false,
            LastServicedFloor = null,
            LastServicedAtUtc = null
        };

        for (var floor = 1; floor <= session.FloorCount; floor++)
        {
            session.FloorCalls.Add(new FloorCall
            {
                SimulationSessionId = session.Id,
                FloorNumber = floor,
                IsPressed = false
            });

            session.CabinRequests.Add(new CabinRequest
            {
                SimulationSessionId = session.Id,
                FloorNumber = floor,
                IsPressed = false
            });
        }

        foreach (var passengerRequest in request.InitialPassengers)
        {
            session.Passengers.Add(new Passenger
            {
                SimulationSessionId = session.Id,
                WeightKg = passengerRequest.WeightKg,
                SourceFloor = passengerRequest.SourceFloor,
                TargetFloor = passengerRequest.TargetFloor,
                Status = PassengerStatus.Created,
                CurrentFloor = passengerRequest.SourceFloor
            });
        }

        _dbContext.SimulationSessions.Add(session);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "SessionCreated",
            nameof(SimulationSession),
            session.Id.ToString(),
            $"Создан сеанс '{session.Name}' с {session.FloorCount} этажами.",
            cancellationToken);

        return _snapshotBuilder.Build(session);
    }

    public async Task<SessionSnapshotDto> StartAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var anyRunningSession = await _dbContext.SimulationSessions
            .AnyAsync(
                x => x.Id != sessionId && x.Status == SessionStatus.Running,
                cancellationToken);

        if (anyRunningSession)
        {
            throw new ConflictApplicationException("В системе уже выполняется другой активный сеанс.");
        }

        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        if (session.Status != SessionStatus.Draft)
        {
            throw new ConflictApplicationException("Запустить можно только сеанс в состоянии черновика.");
        }

        var nowUtc = _clock.UtcNow;

        session.Status = SessionStatus.Running;
        session.StartedAtUtc = nowUtc;
        session.StopRequested = false;
        session.ElevatorState.GoCommandPending = false;

        _simulationOperations.InitializePassengersOnSessionStart(session, nowUtc);
        _simulationOperations.UpdateCurrentLoad(session);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "SessionStarted",
            nameof(SimulationSession),
            session.Id.ToString(),
            $"Запущен сеанс '{session.Name}'.",
            cancellationToken);

        var snapshot = _snapshotBuilder.Build(session);

        await _simulationNotifier.BroadcastSnapshotAsync(snapshot, cancellationToken);

        return snapshot;
    }

    public async Task<SessionSnapshotDto> StopAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        if (session.Status != SessionStatus.Running)
        {
            throw new ConflictApplicationException("Остановить можно только запущенный сеанс.");
        }

        _simulationOperations.UpdateCurrentLoad(session);

        if (session.ElevatorState.CurrentLoadKg > 0m)
        {
            throw new ConflictApplicationException(
                "Остановить систему можно только в том случае, если лифт пустой.");
        }

        session.StopRequested = true;

        var stoppedImmediately = session.ElevatorState.MovementState is
            ElevatorMovementState.IdleClosed
            or ElevatorMovementState.IdleOpen;

        if (stoppedImmediately)
        {
            _simulationOperations.FinalizeStop(session, _clock.UtcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            stoppedImmediately ? "SessionStopped" : "SessionStopRequested",
            nameof(SimulationSession),
            session.Id.ToString(),
            stoppedImmediately
                ? $"Сеанс '{session.Name}' остановлен."
                : $"Для сеанса '{session.Name}' запрошена остановка на ближайшем допустимом состоянии.",
            cancellationToken);

        var snapshot = _snapshotBuilder.Build(session);

        await _simulationNotifier.BroadcastSnapshotAsync(snapshot, cancellationToken);

        return snapshot;
    }

    public async Task<SessionSnapshotDto> IssueGoCommandAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        if (session.Status != SessionStatus.Running)
        {
            throw new ConflictApplicationException("Команду «Ход» можно подать только для запущенного сеанса.");
        }

        if (session.ElevatorState.MovementState is ElevatorMovementState.MovingUp
            or ElevatorMovementState.MovingDown)
        {
            throw new ConflictApplicationException("Лифт уже находится в движении.");
        }

        if (session.ElevatorState.ActiveTripId.HasValue)
        {
            throw new ConflictApplicationException(
                "Команда «Ход» не требуется: текущая поездка будет продолжена автоматически.");
        }

        if (session.ElevatorState.GoCommandPending)
        {
            throw new ConflictApplicationException("Команда «Ход» уже принята системой и ожидает выполнения.");
        }

        var hasAnyRequests = session.FloorCalls.Any(x => x.IsPressed)
            || session.CabinRequests.Any(x => x.IsPressed);

        if (!hasAnyRequests)
        {
            throw new ConflictApplicationException("Для выполнения команды «Ход» отсутствуют активные заявки.");
        }

        var hasRequestsOutsideCurrentFloor = CurrentFloorServiceInspector.HasRequestsOutsideCurrentFloor(
            session,
            session.ElevatorState.CurrentFloor);

        if (!hasRequestsOutsideCurrentFloor)
        {
            throw new ConflictApplicationException(
                "Команда «Ход» требуется только при наличии заявок на движение за пределы текущего этажа.");
        }

        if (CurrentFloorServiceInspector.ShouldAutoServeCurrentFloorBeforeMovement(session))
        {
            throw new ConflictApplicationException(
                "Текущий этаж еще не обслужен. Команда «Ход» станет доступна после завершения остановки.");
        }

        _simulationOperations.RequestGoCommand(session, _clock.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "GoCommandIssued",
            nameof(SimulationSession),
            session.Id.ToString(),
            $"Для сеанса '{session.Name}' подана команда «Ход».",
            cancellationToken);

        var snapshot = _snapshotBuilder.Build(session);

        await _simulationNotifier.BroadcastSnapshotAsync(snapshot, cancellationToken);

        return snapshot;
    }

    public async Task DeleteAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SimulationSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new NotFoundApplicationException("Сеанс не найден.");
        }

        if (session.Status == SessionStatus.Running)
        {
            throw new ConflictApplicationException("Нельзя удалить запущенный сеанс.");
        }

        _dbContext.SimulationSessions.Remove(session);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "SessionDeleted",
            nameof(SimulationSession),
            session.Id.ToString(),
            $"Удален сеанс '{session.Name}'.",
            cancellationToken);
    }

    private async Task<SimulationSession> GetAggregateOrThrowAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SimulationSessions
            .IncludeAggregate()
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new NotFoundApplicationException("Сеанс не найден.");
        }

        return session;
    }

    private static void ValidatePassengerRequests(
        IEnumerable<CreatePassengerRequestDto> requests,
        int floorCount)
    {
        foreach (var passenger in requests)
        {
            ValidatePassenger(passenger.WeightKg, passenger.SourceFloor, passenger.TargetFloor, floorCount);
        }
    }

    internal static void ValidatePassenger(
        decimal weightKg,
        int sourceFloor,
        int targetFloor,
        int floorCount)
    {
        if (sourceFloor < 1 || sourceFloor > floorCount)
        {
            throw new ValidationApplicationException("Этаж появления пассажира выходит за пределы количества этажей.");
        }

        if (targetFloor < 1 || targetFloor > floorCount)
        {
            throw new ValidationApplicationException("Целевой этаж пассажира выходит за пределы количества этажей.");
        }

        if (sourceFloor == targetFloor)
        {
            throw new ValidationApplicationException("Этаж появления и целевой этаж не должны совпадать.");
        }

        if (weightKg <= 0m || weightKg > 400m)
        {
            throw new ValidationApplicationException("Вес пассажира должен быть больше 0 и не превышать 400 кг.");
        }
    }
}

public sealed class PassengerService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ISessionSnapshotBuilder _snapshotBuilder;
    private readonly ISimulationNotifier _simulationNotifier;
    private readonly IClock _clock;

    public PassengerService(
        IApplicationDbContext dbContext,
        IAuditService auditService,
        ISessionSnapshotBuilder snapshotBuilder,
        ISimulationNotifier simulationNotifier,
        IClock clock)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _snapshotBuilder = snapshotBuilder;
        _simulationNotifier = simulationNotifier;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PassengerDto>> GetAllAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);
        var snapshot = _snapshotBuilder.Build(session);

        return snapshot.Passengers;
    }

    public async Task<SessionSnapshotDto> CreateAsync(
        Guid sessionId,
        CreatePassengerRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        if (session.Status == SessionStatus.Stopped)
        {
            throw new ConflictApplicationException("Нельзя добавить пассажира в остановленный сеанс.");
        }

        SessionService.ValidatePassenger(
            request.WeightKg,
            request.SourceFloor,
            request.TargetFloor,
            session.FloorCount);

        var passenger = new Passenger
        {
            SimulationSessionId = session.Id,
            WeightKg = request.WeightKg,
            SourceFloor = request.SourceFloor,
            TargetFloor = request.TargetFloor,
            Status = PassengerStatus.Created,
            CurrentFloor = request.SourceFloor,
            CallPressPlannedAtUtc = session.Status == SessionStatus.Running
                ? _clock.UtcNow.AddSeconds(DomainConstants.PassengerCallDelaySeconds)
                : null
        };

        session.Passengers.Add(passenger);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "PassengerCreated",
            nameof(Passenger),
            passenger.Id.ToString(),
            $"Создан пассажир весом {passenger.WeightKg} кг: этаж {passenger.SourceFloor} -> {passenger.TargetFloor}.",
            cancellationToken);

        var snapshot = _snapshotBuilder.Build(session);

        await _simulationNotifier.BroadcastSnapshotAsync(snapshot, cancellationToken);

        return snapshot;
    }

    public async Task<SessionSnapshotDto> UpdateAsync(
        Guid sessionId,
        Guid passengerId,
        UpdatePassengerRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        if (session.Status != SessionStatus.Draft)
        {
            throw new ConflictApplicationException("Редактирование пассажиров доступно только в черновике сеанса.");
        }

        var passenger = session.Passengers.FirstOrDefault(x => x.Id == passengerId);

        if (passenger is null)
        {
            throw new NotFoundApplicationException("Пассажир не найден.");
        }

        if (passenger.Status != PassengerStatus.Created)
        {
            throw new ConflictApplicationException("Можно редактировать только еще не обработанного пассажира.");
        }

        SessionService.ValidatePassenger(
            request.WeightKg,
            request.SourceFloor,
            request.TargetFloor,
            session.FloorCount);

        passenger.WeightKg = request.WeightKg;
        passenger.SourceFloor = request.SourceFloor;
        passenger.TargetFloor = request.TargetFloor;
        passenger.CurrentFloor = request.SourceFloor;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "PassengerUpdated",
            nameof(Passenger),
            passenger.Id.ToString(),
            $"Обновлен пассажир: этаж {passenger.SourceFloor} -> {passenger.TargetFloor}, вес {passenger.WeightKg} кг.",
            cancellationToken);

        return _snapshotBuilder.Build(session);
    }

    public async Task<SessionSnapshotDto> DeleteAsync(
        Guid sessionId,
        Guid passengerId,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);

        if (session.Status != SessionStatus.Draft)
        {
            throw new ConflictApplicationException("Удаление пассажиров доступно только в черновике сеанса.");
        }

        var passenger = session.Passengers.FirstOrDefault(x => x.Id == passengerId);

        if (passenger is null)
        {
            throw new NotFoundApplicationException("Пассажир не найден.");
        }

        if (passenger.Status != PassengerStatus.Created)
        {
            throw new ConflictApplicationException("Можно удалить только еще не обработанного пассажира.");
        }

        session.Passengers.Remove(passenger);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "PassengerDeleted",
            nameof(Passenger),
            passenger.Id.ToString(),
            "Пассажир удален из сеанса.",
            cancellationToken);

        return _snapshotBuilder.Build(session);
    }

    public async Task<PassengerLocationDto> GetLocationAsync(
        Guid sessionId,
        Guid passengerId,
        CancellationToken cancellationToken)
    {
        var session = await GetAggregateOrThrowAsync(sessionId, cancellationToken);
        var snapshot = _snapshotBuilder.Build(session);
        var passenger = snapshot.Passengers.FirstOrDefault(x => x.Id == passengerId);

        if (passenger is null)
        {
            throw new NotFoundApplicationException("Пассажир не найден.");
        }

        await _auditService.WriteAsync(
            "PassengerLocationRequested",
            nameof(Passenger),
            passenger.Id.ToString(),
            $"Выполнен опрос текущего положения пассажира '{passenger.Id}' в сеансе '{session.Id}'.",
            cancellationToken);

        return new PassengerLocationDto
        {
            PassengerId = passenger.Id,
            LocationDescription = passenger.LocationDescription,
            Status = passenger.Status,
            CurrentFloor = passenger.CurrentFloor
        };
    }

    private async Task<SimulationSession> GetAggregateOrThrowAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SimulationSessions
            .IncludeAggregate()
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new NotFoundApplicationException("Сеанс не найден.");
        }

        return session;
    }
}

public sealed class ReportService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IReportExportService _reportExportService;
    private readonly IAuditService _auditService;

    public ReportService(
        IApplicationDbContext dbContext,
        IReportExportService reportExportService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _reportExportService = reportExportService;
        _auditService = auditService;
    }

    public async Task<SessionReportDto> GetAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetStoppedSessionOrThrowAsync(sessionId, cancellationToken);

        return Map(session);
    }

    public async Task<byte[]> ExportExcelAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await GetStoppedSessionOrThrowAsync(sessionId, cancellationToken);

        if (session.Report is null)
        {
            throw new ConflictApplicationException("Для выбранного сеанса итоговый отчет пока не сформирован.");
        }

        var fileBytes = await _reportExportService.ExportExcelAsync(
            session,
            session.Report,
            cancellationToken);

        await _auditService.WriteAsync(
            "ReportExcelExported",
            nameof(SessionReport),
            session.Report.Id.ToString(),
            $"Выполнен экспорт итогового отчета по сеансу '{session.Name}' в Excel.",
            cancellationToken);

        return fileBytes;
    }

    private async Task<SimulationSession> GetStoppedSessionOrThrowAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.SimulationSessions
            .IncludeAggregate()
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new NotFoundApplicationException("Сеанс не найден.");
        }

        if (session.Status != SessionStatus.Stopped)
        {
            throw new ConflictApplicationException("Итоговый отчет доступен только после остановки системы.");
        }

        if (session.Report is null)
        {
            throw new ConflictApplicationException("Для выбранного сеанса итоговый отчет еще не сформирован.");
        }

        return session;
    }

    private static SessionReportDto Map(SimulationSession session)
    {
        var report = session.Report!;

        return new SessionReportDto
        {
            SessionId = session.Id,
            SessionName = session.Name,
            TotalTrips = report.TotalTrips,
            EmptyTrips = report.EmptyTrips,
            TotalTransportedWeightKg = report.TotalTransportedWeightKg,
            TotalCreatedPassengers = report.TotalCreatedPassengers,
            SessionStartedAtUtc = session.StartedAtUtc,
            SessionStoppedAtUtc = session.StoppedAtUtc,
            GeneratedAtUtc = report.GeneratedAtUtc,
            Passengers = session.Passengers
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => PassengerPresentationMapper.Map(session, x))
                .ToList()
        };
    }
}

public sealed class AuditQueryService
{
    private readonly IApplicationDbContext _dbContext;

    public AuditQueryService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAsync(
        AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit <= 0 ? 200 : query.Limit, 1, 1000);

        var auditQuery = _dbContext.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            auditQuery = auditQuery.Where(x => x.Username.Contains(query.Username));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditQuery = auditQuery.Where(x => x.Action.Contains(query.Action));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            auditQuery = auditQuery.Where(x => x.EntityType.Contains(query.EntityType));
        }

        if (query.SessionId.HasValue)
        {
            var sessionIdText = query.SessionId.Value.ToString();

            auditQuery = auditQuery.Where(
                x => x.EntityId == sessionIdText || x.Details.Contains(sessionIdText));
        }

        if (query.FromUtc.HasValue)
        {
            auditQuery = auditQuery.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            auditQuery = auditQuery.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);
        }

        return await auditQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Username = x.Username,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Details = x.Details,
                IpAddress = x.IpAddress,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
