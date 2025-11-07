using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using MediatR;

namespace Enrollments.Application.Commands.CancelEnrollment;

/// <summary>
/// 履修登録をキャンセルするハンドラー
/// </summary>
public class CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand>
{
    private readonly IEnrollmentRepository _enrollmentRepository;

    public CancelEnrollmentCommandHandler(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }

    public async Task Handle(CancelEnrollmentCommand request, CancellationToken cancellationToken)
    {
        // 1. 履修登録を取得
        var enrollmentId = new EnrollmentId(request.EnrollmentId);
        var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId, cancellationToken);

        if (enrollment == null)
        {
            throw new NotFoundException($"履修登録ID {request.EnrollmentId} が見つかりません");
        }

        // 2. 履修登録をキャンセル
        enrollment.Cancel(request.CancelledBy, request.Reason);

        // 3. 変更を保存
        _enrollmentRepository.Update(enrollment);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);
    }
}
