using Enrollments.Application.Commands.CreateCourse;
using Enrollments.Application.Queries.GetCourseByCode;
using Enrollments.Application.Queries.GetCourses;
using Microsoft.AspNetCore.Mvc;

namespace Enrollments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICreateCourseService _createCourseService;
    private readonly IGetCoursesService _getCoursesService;
    private readonly IGetCourseByCodeService _getCourseByCodeService;

    public CoursesController(
        ICreateCourseService createCourseService,
        IGetCoursesService getCoursesService,
        IGetCourseByCodeService getCourseByCodeService)
    {
        _createCourseService = createCourseService;
        _getCoursesService = getCoursesService;
        _getCourseByCodeService = getCourseByCodeService;
    }

    /// <summary>
    /// コース一覧取得
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CourseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseDto>>> GetCourses()
    {
        var result = await _getCoursesService.GetCoursesAsync();
        return Ok(result);
    }

    /// <summary>
    /// コード指定でコース取得
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(CourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDto>> GetCourseByCode(string code)
    {
        var result = await _getCourseByCodeService.GetCourseByCodeAsync(code);

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
    public async Task<ActionResult<CreateCourseResponse>> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var command = new CreateCourseCommand
        {
            CourseCode = request.CourseCode,
            Name = request.Name,
            Credits = request.Credits,
            MaxCapacity = request.MaxCapacity
        };

        var courseCode = await _createCourseService.CreateCourseAsync(command);

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
