using Enrollments.Application.Commands.CreateStudent;
using Enrollments.Application.Commands.UpdateStudent;
using Enrollments.Application.Queries.GetStudents;
using Enrollments.Application.Queries.Students;
using Enrollments.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Enrollments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly ICreateStudentService _createStudentService;
    private readonly IUpdateStudentService _updateStudentService;
    private readonly IGetStudentsService _getStudentsService;

    public StudentsController(
        ICreateStudentService createStudentService,
        IUpdateStudentService updateStudentService,
        IGetStudentsService getStudentsService)
    {
        _createStudentService = createStudentService;
        _updateStudentService = updateStudentService;
        _getStudentsService = getStudentsService;
    }

    /// <summary>
    /// 学生一覧取得
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StudentDto>>> GetStudents([FromQuery] int? grade, [FromQuery] string? name, [FromQuery] string? email)
    {
        var query = new GetStudentsQuery
        {
            Grade = grade,
            Name = name,
            Email = email
        };

        var students = await _getStudentsService.GetStudentsAsync(query);

        return Ok(students);
    }

    /// <summary>
    /// 学生登録
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateStudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateStudentResponse>> CreateStudent([FromBody] CreateStudentRequest request)
    {
        try
        {
            var command = new CreateStudentCommand
            {
                Name = request.Name,
                Email = request.Email,
                Grade = request.Grade
            };

            var studentId = await _createStudentService.CreateStudentAsync(command);

            return CreatedAtAction(
                nameof(GetStudent),
                new { studentId = studentId },
                new CreateStudentResponse(studentId));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 学生取得（プレースホルダー）
    /// </summary>
    [HttpGet("{studentId}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<StudentDto> GetStudent(Guid studentId)
    {
        // US-S02で実装予定
        return StatusCode(StatusCodes.Status501NotImplemented);
    }

    /// <summary>
    /// 学生情報更新
    /// </summary>
    [HttpPut("{studentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudent(Guid studentId, [FromBody] UpdateStudentRequest request)
    {
        try
        {
            var command = new UpdateStudentCommand
            {
                StudentId = studentId,
                Name = request.Name,
                Email = request.Email,
                Grade = request.Grade
            };

            await _updateStudentService.UpdateStudentAsync(command);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

public record CreateStudentRequest(
    string Name,
    string Email,
    int Grade);

public record CreateStudentResponse(Guid StudentId);

public record UpdateStudentRequest(
    string Name,
    string Email,
    int Grade);
