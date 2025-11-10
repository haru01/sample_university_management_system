using StudentRegistrations.Domain.StudentAggregate;
using StudentRegistrations.Domain.Exceptions;
using MediatR;
using Shared.ValueObjects;

namespace StudentRegistrations.Application.Commands.DeleteStudent;

/// <summary>
/// 学生削除コマンドハンドラー
/// </summary>
public class DeleteStudentCommandHandler : IRequestHandler<DeleteStudentCommand>
{
    private readonly IStudentRepository _studentRepository;

    public DeleteStudentCommandHandler(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task Handle(DeleteStudentCommand request, CancellationToken cancellationToken)
    {
        // 学生IDで学生を取得
        var student = await _studentRepository.GetByIdAsync(new StudentId(request.StudentId), cancellationToken);
        if (student == null)
            throw new NotFoundException("STUDENT_NOT_FOUND", "Student not found");

        // 学生を削除
        await _studentRepository.DeleteAsync(student, cancellationToken);
        await _studentRepository.SaveChangesAsync(cancellationToken);
    }
}
