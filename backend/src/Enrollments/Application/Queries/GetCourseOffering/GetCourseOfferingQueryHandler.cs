using Enrollments.Application.Queries.CourseOfferings;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetCourseOffering;

/// <summary>
/// コース開講詳細取得クエリハンドラー
/// </summary>
public class GetCourseOfferingQueryHandler : IRequestHandler<GetCourseOfferingQuery, CourseOfferingDto>
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ICourseRepository _courseRepository;

    public GetCourseOfferingQueryHandler(
        ICourseOfferingRepository courseOfferingRepository,
        ICourseRepository courseRepository)
    {
        _courseOfferingRepository = courseOfferingRepository;
        _courseRepository = courseRepository;
    }

    public async Task<CourseOfferingDto> Handle(
        GetCourseOfferingQuery request,
        CancellationToken cancellationToken)
    {
        // コース開講を取得
        var offeringId = new OfferingId(request.OfferingId);
        var offering = await _courseOfferingRepository.GetByIdAsync(offeringId, cancellationToken);
        if (offering == null)
            throw new KeyNotFoundException($"CourseOffering not found: {request.OfferingId}");

        // コースマスタ情報を取得
        var course = await _courseRepository.GetByCodeAsync(offering.CourseCode, cancellationToken);
        var courseName = course?.Name ?? "Unknown";

        return new CourseOfferingDto
        {
            OfferingId = offering.Id.Value,
            CourseCode = offering.CourseCode.Value,
            CourseName = courseName,
            Year = offering.SemesterId.Year,
            Period = offering.SemesterId.Period,
            Credits = offering.Credits,
            MaxCapacity = offering.MaxCapacity,
            Instructor = offering.Instructor,
            Status = offering.Status.ToString()
        };
    }
}
