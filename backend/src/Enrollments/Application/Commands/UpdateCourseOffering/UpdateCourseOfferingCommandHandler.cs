using Enrollments.Domain.CourseOfferingAggregate;
using MediatR;

namespace Enrollments.Application.Commands.UpdateCourseOffering;

/// <summary>
/// コース開講更新コマンドハンドラー
/// </summary>
public class UpdateCourseOfferingCommandHandler : IRequestHandler<UpdateCourseOfferingCommand, Unit>
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;

    public UpdateCourseOfferingCommandHandler(ICourseOfferingRepository courseOfferingRepository)
    {
        _courseOfferingRepository = courseOfferingRepository;
    }

    public async Task<Unit> Handle(UpdateCourseOfferingCommand request, CancellationToken cancellationToken)
    {
        // コース開講を取得
        var offeringId = new OfferingId(request.OfferingId);
        var courseOffering = await _courseOfferingRepository.GetByIdAsync(offeringId, cancellationToken);
        if (courseOffering == null)
            throw new KeyNotFoundException($"CourseOffering not found: {request.OfferingId}");

        // コース開講情報を更新
        courseOffering.Update(request.Credits, request.MaxCapacity, request.Instructor);

        // 永続化
        _courseOfferingRepository.Update(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
