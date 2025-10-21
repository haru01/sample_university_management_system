using Enrollments.Domain.StudentAggregate;
using Enrollments.Domain.Exceptions;

namespace Enrollments.Application.Commands.UpdateStudent;

/// <summary>
/// 学生情報更新サービス実装
/// </summary>
public class UpdateStudentService : IUpdateStudentService
{
    private readonly IStudentRepository _studentRepository;

    public UpdateStudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task UpdateStudentAsync(UpdateStudentCommand command)
    {
        // 学生IDで学生を取得
        var student = await _studentRepository.GetByIdAsync(new StudentId(command.StudentId));
        if (student == null)
            throw new NotFoundException("STUDENT_NOT_FOUND", "Student not found");

        // 他の学生が同じメールアドレスを使用していないかチェック
        var existingStudent = await _studentRepository.GetByEmailAsync(command.Email);
        if (existingStudent != null && existingStudent.Id.Value != command.StudentId)
            throw new ConflictException("STUDENT_EMAIL_ALREADY_EXISTS", "Email already exists");

        // 学生情報を更新
        student.Update(
            command.Name,
            command.Email,
            command.Grade);

        // 永続化
        await _studentRepository.SaveChangesAsync();
    }
}
