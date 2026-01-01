using Enrollments.Application.Queries.CourseOfferings;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Enrollments.Application.Queries.SelectCourseOfferingsBySemester;

/// <summary>
/// 学期ごとのコース開講一覧取得クエリハンドラー
/// </summary>
public class SelectCourseOfferingsBySemesterQueryHandler
    : IRequestHandler<SelectCourseOfferingsBySemesterQuery, List<CourseOfferingDto>>
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ICourseRepository _courseRepository;

    public SelectCourseOfferingsBySemesterQueryHandler(
        ICourseOfferingRepository courseOfferingRepository,
        ICourseRepository courseRepository)
    {
        _courseOfferingRepository = courseOfferingRepository;
        _courseRepository = courseRepository;
    }

    public async Task<List<CourseOfferingDto>> Handle(
        SelectCourseOfferingsBySemesterQuery request,
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

        // コースマスタ情報を一括取得（N+1問題を回避）
        var courseCodes = courseOfferings.Select(o => o.CourseCode).Distinct().ToList();
        var courses = await _courseRepository.GetByCodesAsync(courseCodes, cancellationToken);
        var courseDict = courses.ToDictionary(c => c.Id);

        // CourseOfferingDtoに変換
        var dtos = courseOfferings.Select(offering =>
        {
            var courseName = courseDict.TryGetValue(offering.CourseCode, out var course)
                ? course.Name
                : "Unknown";

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
        }).ToList();

        return dtos;
    }
}
