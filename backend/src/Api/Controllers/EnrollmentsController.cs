using Enrollments.Application.Commands.CancelEnrollment;
using Enrollments.Application.Commands.CompleteEnrollment;
using Enrollments.Application.Commands.EnrollStudent;
using Enrollments.Application.Queries.Enrollments;
using Enrollments.Application.Queries.GetStudentEnrollments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrollmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 学生の履修登録一覧取得
    /// </summary>
    [HttpGet("students/{studentId:guid}")]
    [ProducesResponseType(typeof(List<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrollmentDto>>> GetStudentEnrollments(
        Guid studentId,
        [FromQuery] string? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStudentEnrollmentsQuery
        {
            StudentId = studentId,
            StatusFilter = statusFilter
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 履修登録 (学生をコース開講に登録)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EnrollStudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollStudentResponse>> EnrollStudent(
        [FromBody] EnrollStudentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EnrollStudentCommand
        {
            StudentId = request.StudentId,
            OfferingId = request.OfferingId,
            EnrolledBy = request.EnrolledBy,
            InitialNote = request.InitialNote
        };

        var enrollmentId = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetStudentEnrollments),
            new { studentId = request.StudentId },
            new EnrollStudentResponse(enrollmentId));
    }

    /// <summary>
    /// 履修登録完了 (仮登録 → 本登録)
    /// </summary>
    [HttpPost("{enrollmentId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteEnrollment(
        Guid enrollmentId,
        [FromBody] CompleteEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CompleteEnrollmentCommand
        {
            EnrollmentId = enrollmentId,
            CompletedBy = request.CompletedBy,
            Reason = request.Reason
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 履修登録キャンセル
    /// </summary>
    [HttpPost("{enrollmentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelEnrollment(
        Guid enrollmentId,
        [FromBody] CancelEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = enrollmentId,
            CancelledBy = request.CancelledBy,
            Reason = request.Reason
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Request for enrolling a student
/// </summary>
public record EnrollStudentRequest(
    Guid StudentId,
    int OfferingId,
    string EnrolledBy,
    string? InitialNote = null);

/// <summary>
/// Response after enrolling a student
/// </summary>
public record EnrollStudentResponse(Guid EnrollmentId);

/// <summary>
/// Request for completing an enrollment
/// </summary>
public record CompleteEnrollmentRequest(
    string CompletedBy,
    string? Reason = null);

/// <summary>
/// Request for cancelling an enrollment
/// </summary>
public record CancelEnrollmentRequest(
    string CancelledBy,
    string Reason);
