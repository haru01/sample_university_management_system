using Enrollments.Application.Commands.CreateCourse;
using Enrollments.Application.Queries.GetCourseByCode;
using Enrollments.Application.Queries.SelectCourses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Enrollments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoursesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// コース一覧取得
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseDto>>> GetCourses(CancellationToken cancellationToken)
    {
        var query = new SelectCoursesQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// コード指定でコース取得
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourseByCode(string code, CancellationToken cancellationToken)
    {
        var query = new GetCourseByCodeQuery { CourseCode = code };
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return NotFound(new { message = $"Course with code '{code}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// コース登録
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateCourseResponse>> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCourseCommand
        {
            CourseCode = request.CourseCode,
            Name = request.Name,
            Credits = request.Credits,
            MaxCapacity = request.MaxCapacity
        };

        var courseCode = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetCourseByCode),
            new { code = courseCode },
            new CreateCourseResponse(courseCode));
    }
}

public record CreateCourseRequest(
    string CourseCode,
    string Name,
    int Credits,
    int MaxCapacity);

public record CreateCourseResponse(string CourseCode);
