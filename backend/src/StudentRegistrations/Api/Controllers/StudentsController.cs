using StudentRegistrations.Application.Commands.CreateStudent;
using StudentRegistrations.Application.Commands.UpdateStudent;
using StudentRegistrations.Application.Commands.DeleteStudent;
using StudentRegistrations.Application.Queries.GetStudent;
using StudentRegistrations.Application.Queries.SelectStudents;
using StudentRegistrations.Application.Queries.Students;
using StudentRegistrations.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace StudentRegistrations.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 学生一覧取得
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StudentDto>>> GetStudents(
        [FromQuery] int? grade,
        [FromQuery] string? name,
        [FromQuery] string? email,
        CancellationToken cancellationToken)
    {
        var query = new SelectStudentsQuery
        {
            Grade = grade,
            Name = name,
            Email = email
        };

        var students = await _mediator.Send(query, cancellationToken);

        return Ok(students);
    }

    /// <summary>
    /// 学生登録
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateStudentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateStudentResponse>> CreateStudent(
        [FromBody] CreateStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateStudentCommand
            {
                Name = request.Name,
                Email = request.Email,
                Grade = request.Grade
            };

            var studentId = await _mediator.Send(command, cancellationToken);

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
    /// 学生取得
    /// </summary>
    [HttpGet("{studentId}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDto>> GetStudent(Guid studentId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetStudentQuery { StudentId = studentId };
            var student = await _mediator.Send(query, cancellationToken);
            return Ok(student);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 学生情報更新
    /// </summary>
    [HttpPut("{studentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudent(
        Guid studentId,
        [FromBody] UpdateStudentRequest request,
        CancellationToken cancellationToken)
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

            await _mediator.Send(command, cancellationToken);

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

    /// <summary>
    /// 学生削除
    /// </summary>
    [HttpDelete("{studentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudent(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new DeleteStudentCommand { StudentId = studentId };
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
