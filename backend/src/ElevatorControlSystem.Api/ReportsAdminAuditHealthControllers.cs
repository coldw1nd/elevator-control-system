using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElevatorControlSystem.Application;

namespace ElevatorControlSystem.Api;

[ApiController]
[Authorize]
[Route("api/sessions/{sessionId:guid}/report")]
public sealed class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SessionReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionReportDto>> Get(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAsync(sessionId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportExcel(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetAsync(sessionId, cancellationToken);
        var fileBytes = await _reportService.ExportExcelAsync(sessionId, cancellationToken);

        var safeName = string.Join(
            "_",
            report.SessionName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        var fileName = $"report_{safeName}_{report.SessionId}.xlsx";

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly UserManagementService _userManagementService;

    public AdminUsersController(UserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.CreateAsync(request, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(
        Guid userId,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.UpdateAsync(
            userId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{userId:guid}/toggle-activity")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> ToggleActivity(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.ToggleActivityAsync(
            userId,
            cancellationToken);

        return Ok(result);
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly AuditQueryService _auditQueryService;

    public AuditController(AuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> Get(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _auditQueryService.GetAsync(query, cancellationToken);

        return Ok(result);
    }
}

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(
            new
            {
                status = "ok",
                timeUtc = DateTime.UtcNow
            });
    }
}
