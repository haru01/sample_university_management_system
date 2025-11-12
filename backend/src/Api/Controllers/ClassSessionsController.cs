using Attendance.Application.Commands.CreateClassSession;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClassSessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 授業セッション登録
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateClassSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateClassSessionResponse>> CreateClassSession(
        [FromBody] CreateClassSessionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateClassSessionCommand
        {
            OfferingId = request.OfferingId,
            SessionNumber = request.SessionNumber,
            SessionDate = request.SessionDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location,
            Topic = request.Topic
        };

        var sessionId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(CreateClassSession),
            new { id = sessionId },
            new CreateClassSessionResponse(sessionId));
    }
}

public record CreateClassSessionRequest(
    int OfferingId,
    int SessionNumber,
    DateOnly SessionDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Location,
    string? Topic);

public record CreateClassSessionResponse(int SessionId);
