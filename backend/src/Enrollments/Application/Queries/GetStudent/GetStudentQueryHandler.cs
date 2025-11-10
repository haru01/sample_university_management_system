using Enrollments.Application.Queries.Students;
using Enrollments.Domain.StudentAggregate;
using MediatR;
using Shared.ValueObjects;

namespace Enrollments.Application.Queries.GetStudent;

/// <summary>
/// 学生取得クエリハンドラー
/// </summary>
public class GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentDto>
{
    private readonly IStudentRepository _studentRepository;

    public GetStudentQueryHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<StudentDto> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);

        if (student is null)
        {
            throw new KeyNotFoundException($"Student with ID {request.StudentId} not found");
        }

        return new StudentDto
        {
            StudentId = student.Id.Value,
            Name = student.Name,
            Email = student.Email,
            Grade = student.Grade
        };
    }
}
