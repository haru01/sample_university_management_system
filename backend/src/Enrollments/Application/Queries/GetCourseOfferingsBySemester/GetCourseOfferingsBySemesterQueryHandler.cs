using Enrollments.Application.Queries.CourseOfferings;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetCourseOfferingsBySemester;

/// <summary>
/// 学期ごとのコース開講一覧取得クエリハンドラー
/// </summary>
public class GetCourseOfferingsBySemesterQueryHandler
    : IRequestHandler<GetCourseOfferingsBySemesterQuery, List<CourseOfferingDto>>
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ICourseRepository _courseRepository;

    public GetCourseOfferingsBySemesterQueryHandler(
        ICourseOfferingRepository courseOfferingRepository,
        ICourseRepository courseRepository)
    {
        _courseOfferingRepository = courseOfferingRepository;
        _courseRepository = courseRepository;
    }

    public async Task<List<CourseOfferingDto>> Handle(
        GetCourseOfferingsBySemesterQuery request,
        CancellationToken cancellationToken)
    {
        // SemesterIdを構築
        var semesterId = new SemesterId(request.Year, request.Period);

        // StatusFilterのパース
        OfferingStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(request.StatusFilter))
        {
            if (Enum.TryParse<OfferingStatus>(request.StatusFilter, ignoreCase: true, out var status))
            {
                statusFilter = status;
            }
        }

        // コース開講一覧を取得
        var courseOfferings = await _courseOfferingRepository.SelectBySemesterAsync(
            semesterId, statusFilter, cancellationToken);

        // CourseOfferingDtoに変換（コースマスタ情報も含める）
        var dtos = new List<CourseOfferingDto>();
        foreach (var offering in courseOfferings)
        {
            // コースマスタ情報を取得
            var course = await _courseRepository.GetByCodeAsync(offering.CourseCode, cancellationToken);
            var courseName = course?.Name ?? "Unknown";

            dtos.Add(new CourseOfferingDto
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
            });
        }

        return dtos;
    }
}
