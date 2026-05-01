using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElevatorControlSystem.Application;

namespace ElevatorControlSystem.Api;

[ApiController]
[Authorize]
[Route("api/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly SessionService _sessionService;

    public SessionsController(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SessionListItemDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetAllAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> GetCurrent(
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetCurrentAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{sessionId:guid}/snapshot")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> GetSnapshot(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetSnapshotAsync(sessionId, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Create(
        [FromBody] CreateSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.CreateAsync(request, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost("{sessionId:guid}/start")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Start(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.StartAsync(sessionId, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost("{sessionId:guid}/go")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Go(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.IssueGoCommandAsync(sessionId, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost("{sessionId:guid}/stop")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Stop(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.StopAsync(sessionId, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpDelete("{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await _sessionService.DeleteAsync(sessionId, cancellationToken);

        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/sessions/{sessionId:guid}/passengers")]
public sealed class PassengersController : ControllerBase
{
    private readonly PassengerService _passengerService;

    public PassengersController(PassengerService passengerService)
    {
        _passengerService = passengerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PassengerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PassengerDto>>> GetAll(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _passengerService.GetAllAsync(sessionId, cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Create(
        Guid sessionId,
        [FromBody] CreatePassengerRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _passengerService.CreateAsync(
            sessionId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPut("{passengerId:guid}")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Update(
        Guid sessionId,
        Guid passengerId,
        [FromBody] UpdatePassengerRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _passengerService.UpdateAsync(
            sessionId,
            passengerId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpDelete("{passengerId:guid}")]
    [ProducesResponseType(typeof(SessionSnapshotDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionSnapshotDto>> Delete(
        Guid sessionId,
        Guid passengerId,
        CancellationToken cancellationToken)
    {
        var result = await _passengerService.DeleteAsync(
            sessionId,
            passengerId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{passengerId:guid}/location")]
    [ProducesResponseType(typeof(PassengerLocationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PassengerLocationDto>> GetLocation(
        Guid sessionId,
        Guid passengerId,
        CancellationToken cancellationToken)
    {
        var result = await _passengerService.GetLocationAsync(
            sessionId,
            passengerId,
            cancellationToken);

        return Ok(result);
    }
}
