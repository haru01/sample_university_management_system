using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using MediatR;

namespace Enrollments.Application.Commands.CompleteEnrollment;

/// <summary>
/// 履修登録を完了するハンドラー（仮登録 → 本登録）
/// </summary>
public class CompleteEnrollmentCommandHandler : IRequestHandler<CompleteEnrollmentCommand>
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public CompleteEnrollmentCommandHandler(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task Handle(CompleteEnrollmentCommand request, CancellationToken cancellationToken)
    {
        // 1. 履修登録を取得
        var enrollmentId = new EnrollmentId(request.EnrollmentId);
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);

        if (enrollment == null)
        {
            throw new NotFoundException($"履修登録ID {request.EnrollmentId} が見つかりません");
        }

        // 2. 履修登録を完了
        enrollment.Complete(request.CompletedBy, request.Reason);

        // 3. 変更を保存
        _enrollmentRepository.Update(enrollment);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);
    }
}
