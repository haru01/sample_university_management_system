using Enrollments.Domain.CourseOfferingAggregate;
using MediatR;

namespace Enrollments.Application.Commands.CancelCourseOffering;

/// <summary>
/// コース開講キャンセルコマンドハンドラー
/// </summary>
public class CancelCourseOfferingCommandHandler : IRequestHandler<CancelCourseOfferingCommand, Unit>
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;

    public CancelCourseOfferingCommandHandler(ICourseOfferingRepository courseOfferingRepository)
    {
        _courseOfferingRepository = courseOfferingRepository;
    }

    public async Task<Unit> Handle(CancelCourseOfferingCommand request, CancellationToken cancellationToken)
    {
        // コース開講を取得
        var offeringId = new OfferingId(request.OfferingId);
        var courseOffering = await _courseOfferingRepository.GetByIdAsync(offeringId, cancellationToken);
        if (courseOffering == null)
            throw new KeyNotFoundException($"CourseOffering not found: {request.OfferingId}");

        // 履修登録者がいないことを確認
        var hasEnrollments = await _courseOfferingRepository.HasEnrollmentsAsync(
            offeringId, cancellationToken);
        if (hasEnrollments)
            throw new InvalidOperationException("Cannot cancel offering with enrollments");

        // コース開講をキャンセル
        courseOffering.Cancel();

        // 永続化
        _courseOfferingRepository.Update(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
