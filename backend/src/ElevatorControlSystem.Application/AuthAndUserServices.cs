using Microsoft.EntityFrameworkCore;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Application;

public sealed class AuthService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IClock _clock;

    public AuthService(
        IApplicationDbContext dbContext,
        IPasswordHasherService passwordHasherService,
        IJwtTokenService jwtTokenService,
        IAuditService auditService,
        ICurrentUserContext currentUserContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
        _currentUserContext = currentUserContext;
        _clock = clock;
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var submittedUsername = request.Username.Trim();
        var normalizedUsername = submittedUsername.ToLowerInvariant();

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(
                x => x.Username.ToLower() == normalizedUsername,
                cancellationToken);

        if (user is null)
        {
            await _auditService.WriteAsync(
                null,
                submittedUsername,
                "UserLoginFailed",
                nameof(ApplicationUser),
                null,
                "Неуспешная попытка входа: пользователь не найден.",
                ipAddress,
                cancellationToken);

            throw new UnauthorizedApplicationException("Неверное имя пользователя или пароль.");
        }

        if (!user.IsActive)
        {
            await _auditService.WriteAsync(
                user.Id,
                user.Username,
                "UserLoginRejectedInactive",
                nameof(ApplicationUser),
                user.Id.ToString(),
                "Попытка входа отклонена: учетная запись деактивирована.",
                ipAddress,
                cancellationToken);

            throw new ForbiddenApplicationException("Учетная запись пользователя деактивирована.");
        }

        var passwordIsValid = _passwordHasherService.VerifyPassword(
            user,
            request.Password);

        if (!passwordIsValid)
        {
            await _auditService.WriteAsync(
                user.Id,
                user.Username,
                "UserLoginFailed",
                nameof(ApplicationUser),
                user.Id.ToString(),
                "Неуспешная попытка входа: неверный пароль.",
                ipAddress,
                cancellationToken);

            throw new UnauthorizedApplicationException("Неверное имя пользователя или пароль.");
        }

        var token = _jwtTokenService.GenerateToken(user);

        user.LastLoginAtUtc = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            user.Id,
            user.Username,
            "UserLoggedIn",
            nameof(ApplicationUser),
            user.Id.ToString(),
            "Пользователь выполнил вход в систему.",
            ipAddress,
            cancellationToken);

        return new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = MapCurrentUser(user)
        };
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedApplicationException("Пользователь не авторизован.");
        }

        var user = await _dbContext.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == _currentUserContext.UserId.Value,
                cancellationToken);

        if (user is null)
        {
            throw new NotFoundApplicationException("Пользователь не найден.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenApplicationException("Учетная запись пользователя деактивирована.");
        }

        return MapCurrentUser(user);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return;
        }

        await _auditService.WriteAsync(
            "UserLoggedOut",
            nameof(ApplicationUser),
            _currentUserContext.UserId?.ToString(),
            "Пользователь выполнил выход из системы.",
            cancellationToken);
    }

    private static CurrentUserDto MapCurrentUser(ApplicationUser user)
    {
        return new CurrentUserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}

public sealed class UserManagementService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserContext _currentUserContext;

    public UserManagementService(
        IApplicationDbContext dbContext,
        IPasswordHasherService passwordHasherService,
        IAuditService auditService,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _passwordHasherService = passwordHasherService;
        _auditService = auditService;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.ApplicationUsers
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .Select(x => new UserDto
            {
                Id = x.Id,
                Username = x.Username,
                DisplayName = x.DisplayName,
                Role = x.Role,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                LastLoginAtUtc = x.LastLoginAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(
        CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var userExists = await _dbContext.ApplicationUsers
            .AnyAsync(x => x.Username.ToLower() == normalizedUsername, cancellationToken);

        if (userExists)
        {
            throw new ConflictApplicationException("Пользователь с таким логином уже существует.");
        }

        var user = new ApplicationUser
        {
            Username = request.Username.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            IsActive = true
        };

        user.PasswordHash = _passwordHasherService.HashPassword(user, request.Password);

        _dbContext.ApplicationUsers.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "UserCreated",
            nameof(ApplicationUser),
            user.Id.ToString(),
            $"Создан пользователь '{user.Username}' с ролью '{user.Role}'.",
            cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }

    public async Task<UserDto> UpdateAsync(
        Guid userId,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundApplicationException("Пользователь не найден.");
        }

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var duplicateExists = await _dbContext.ApplicationUsers
            .AnyAsync(
                x => x.Id != userId && x.Username.ToLower() == normalizedUsername,
                cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictApplicationException("Пользователь с таким логином уже существует.");
        }

        if (_currentUserContext.UserId == userId && !request.IsActive)
        {
            throw new ConflictApplicationException("Нельзя деактивировать текущую учетную запись.");
        }

        user.Username = request.Username.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = _passwordHasherService.HashPassword(user, request.Password);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "UserUpdated",
            nameof(ApplicationUser),
            user.Id.ToString(),
            $"Обновлен пользователь '{user.Username}'.",
            cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }

    public async Task<UserDto> ToggleActivityAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundApplicationException("Пользователь не найден.");
        }

        if (_currentUserContext.UserId == userId && user.IsActive)
        {
            throw new ConflictApplicationException("Нельзя деактивировать текущую учетную запись.");
        }

        user.IsActive = !user.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.WriteAsync(
            "UserActivityToggled",
            nameof(ApplicationUser),
            user.Id.ToString(),
            $"Изменен признак активности пользователя '{user.Username}' на '{user.IsActive}'.",
            cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}
