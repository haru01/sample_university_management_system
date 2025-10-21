using Enrollments.Domain.StudentAggregate;
using Enrollments.Domain.Exceptions;

namespace Enrollments.Application.Commands.CreateStudent;

/// <summary>
/// 学生作成サービス実装
/// </summary>
public class CreateStudentService : ICreateStudentService
{
    private readonly IStudentRepository _studentRepository;

    public CreateStudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Guid> CreateStudentAsync(CreateStudentCommand command)
    {
        // メールアドレスの重複チェック
        var existing = await _studentRepository.GetByEmailAsync(command.Email);
        if (existing != null)
            throw new ConflictException("STUDENT_EMAIL_ALREADY_EXISTS", "Email already exists");

        // 学生を作成
        var student = Student.Create(
            command.Name,
            command.Email,
            command.Grade);

        // 永続化
        await _studentRepository.AddAsync(student);
        await _studentRepository.SaveChangesAsync();

        return student.Id.Value;
    }
}
