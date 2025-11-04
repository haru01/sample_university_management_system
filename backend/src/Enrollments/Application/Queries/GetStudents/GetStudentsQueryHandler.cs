using Enrollments.Application.Queries.Students;
using Enrollments.Domain.StudentAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetStudents;

/// <summary>
/// 学生一覧取得クエリハンドラー
/// </summary>
public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, List<StudentDto>>
{
    private readonly IStudentRepository _studentRepository;

    public GetStudentsQueryHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<List<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        // Application QueryをDomain Queryに変換
        var domainQuery = new Domain.StudentAggregate.GetStudentsQuery
        {
            Name = request.Name,
            Email = request.Email,
            Grade = request.Grade
        };

        // SQLレベルでフィルタリング・ソート
        var students = await _studentRepository.GetFilteredAsync(domainQuery, cancellationToken);

        // DTO に変換
        return students
            .Select(s => new StudentDto
            {
                StudentId = s.Id.Value,
                Name = s.Name,
                Email = s.Email,
                Grade = s.Grade
            })
            .ToList();
    }
}
