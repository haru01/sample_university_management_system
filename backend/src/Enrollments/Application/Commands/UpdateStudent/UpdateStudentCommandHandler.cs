using Enrollments.Domain.StudentAggregate;
using Enrollments.Domain.Exceptions;
using MediatR;

namespace Enrollments.Application.Commands.UpdateStudent;

/// <summary>
/// 学生情報更新コマンドハンドラー
/// </summary>
public class UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand>
{
    private readonly IStudentRepository _studentRepository;

    public UpdateStudentCommandHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        // 学生IDで学生を取得
        var student = await _studentRepository.GetByIdAsync(new StudentId(request.StudentId), cancellationToken);
        if (student == null)
            throw new NotFoundException("STUDENT_NOT_FOUND", "Student not found");

        // 他の学生が同じメールアドレスを使用していないかチェック
        var existingStudent = await _studentRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingStudent != null && existingStudent.Id.Value != request.StudentId)
            throw new ConflictException("STUDENT_EMAIL_ALREADY_EXISTS", "Email already exists");

        // 学生情報を更新
        student.Update(
            request.Name,
            request.Email,
            request.Grade);

        // 永続化
        await _studentRepository.SaveChangesAsync(cancellationToken);
    }
}
