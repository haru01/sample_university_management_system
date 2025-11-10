using Enrollments.Domain.StudentAggregate;
using Enrollments.Domain.Exceptions;
using MediatR;
using Shared.ValueObjects;

namespace Enrollments.Application.Commands.CreateStudent;

/// <summary>
/// 学生作成コマンドハンドラー
/// </summary>
public class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Guid>
{
    private readonly IStudentRepository _studentRepository;

    public CreateStudentCommandHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<Guid> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {
        // メールアドレスの重複チェック
        var existing = await _studentRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            throw new ConflictException("STUDENT_EMAIL_ALREADY_EXISTS", "Email already exists");

        // 学生を作成
        var student = Student.Create(
            request.Name,
            request.Email,
            request.Grade);

        // 永続化
        await _studentRepository.AddAsync(student, cancellationToken);
        await _studentRepository.SaveChangesAsync(cancellationToken);

        return student.Id.Value;
    }
}
