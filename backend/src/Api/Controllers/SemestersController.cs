using Enrollments.Application.Commands.CreateSemester;
using Enrollments.Application.Queries.GetCurrentSemester;
using Enrollments.Application.Queries.SelectSemesters;
using Enrollments.Application.Queries.Semesters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemestersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SemestersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 学期一覧取得
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SemesterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SemesterDto>>> GetSemesters(CancellationToken cancellationToken)
    {
        var query = new SelectSemestersQuery();
        var semesters = await _mediator.Send(query, cancellationToken);
        return Ok(semesters);
    }

    /// <summary>
    /// 現在の学期取得
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(SemesterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SemesterDto>> GetCurrentSemester(CancellationToken cancellationToken)
    {
        var query = new GetCurrentSemesterQuery();
        var semester = await _mediator.Send(query, cancellationToken);
        return Ok(semester);
    }

    /// <summary>
    /// 学期登録
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateSemesterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateSemesterResponse>> CreateSemester(
        [FromBody] CreateSemesterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSemesterCommand
        {
            Year = request.Year,
            Period = request.Period,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        var semesterId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetSemesters),
            new CreateSemesterResponse(semesterId.Year, semesterId.Period));
    }
}

public record CreateSemesterRequest(
    int Year,
    string Period,
    DateTime StartDate,
    DateTime EndDate);

public record CreateSemesterResponse(int Year, string Period);
