using Enrollments.Application.Queries.Students;
using Enrollments.Domain.StudentAggregate;

namespace Enrollments.Application.Queries.GetStudents;

/// <summary>
/// 学生一覧取得サービス実装
/// </summary>
public class GetStudentsService : IGetStudentsService
{
    private readonly IStudentRepository _studentRepository;

    public GetStudentsService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<List<StudentDto>> GetStudentsAsync(GetStudentsQuery query)
    {
        // SQLレベルでフィルタリング・ソート
        var students = await _studentRepository.GetFilteredAsync(query);

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
