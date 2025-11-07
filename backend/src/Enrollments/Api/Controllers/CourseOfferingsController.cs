using Enrollments.Application.Commands.CancelCourseOffering;
using Enrollments.Application.Commands.CreateCourseOffering;
using Enrollments.Application.Commands.UpdateCourseOffering;
using Enrollments.Application.Queries.CourseOfferings;
using Enrollments.Application.Queries.GetCourseOffering;
using Enrollments.Application.Queries.SelectCourseOfferingsBySemester;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Enrollments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseOfferingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CourseOfferingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 学期ごとのコース開講一覧取得
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseOfferingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseOfferingDto>>> GetCourseOfferingsBySemester(
        [FromQuery] int year,
        [FromQuery] string period,
        [FromQuery] string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SelectCourseOfferingsBySemesterQuery
        {
            Year = year,
            Period = period,
            StatusFilter = statusFilter
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// コース開講詳細取得
    /// </summary>
    [HttpGet("{offeringId:int}")]
    [ProducesResponseType(typeof(CourseOfferingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseOfferingDto>> GetCourseOffering(
        int offeringId,
        CancellationToken cancellationToken)
    {
        var query = new GetCourseOfferingQuery { OfferingId = offeringId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// コース開講登録
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCourseOfferingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCourseOfferingResponse>> CreateCourseOffering(
        [FromBody] CreateCourseOfferingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCourseOfferingCommand
        {
            CourseCode = request.CourseCode,
            Year = request.Year,
            Period = request.Period,
            Credits = request.Credits,
            MaxCapacity = request.MaxCapacity,
            Instructor = request.Instructor
        };

        var offeringId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetCourseOffering),
            new { offeringId },
            new CreateCourseOfferingResponse(offeringId));
    }

    /// <summary>
    /// コース開講更新
    /// </summary>
    [HttpPut("{offeringId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCourseOffering(
        int offeringId,
        [FromBody] UpdateCourseOfferingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = offeringId,
            Credits = request.Credits,
            MaxCapacity = request.MaxCapacity,
            Instructor = request.Instructor
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// コース開講キャンセル
    /// </summary>
    [HttpDelete("{offeringId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelCourseOffering(
        int offeringId,
        CancellationToken cancellationToken)
    {
        var command = new CancelCourseOfferingCommand { OfferingId = offeringId };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

public record CreateCourseOfferingRequest(
    string CourseCode,
    int Year,
    string Period,
    int Credits,
    int MaxCapacity,
    string? Instructor);

public record CreateCourseOfferingResponse(int OfferingId);

public record UpdateCourseOfferingRequest(
    int Credits,
    int MaxCapacity,
    string? Instructor);
