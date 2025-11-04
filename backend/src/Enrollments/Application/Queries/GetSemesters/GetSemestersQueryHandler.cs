using Enrollments.Application.Queries.Semesters;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetSemesters;

/// <summary>
/// 学期一覧取得クエリハンドラー
/// </summary>
public class GetSemestersQueryHandler : IRequestHandler<GetSemestersQuery, List<SemesterDto>>
{
    private readonly ISemesterRepository _semesterRepository;

    public GetSemestersQueryHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<List<SemesterDto>> Handle(GetSemestersQuery request, CancellationToken cancellationToken)
    {
        var semesters = await _semesterRepository.GetAllAsync(cancellationToken);

        return semesters
            .OrderByDescending(s => s.Id.Year)
            .ThenBy(s => s.Id.Period == "Spring" ? 1 : 0) // Fall first, then Spring
            .Select(s => new SemesterDto
            {
                Year = s.Id.Year,
                Period = s.Id.Period,
                StartDate = s.StartDate,
                EndDate = s.EndDate
            })
            .ToList();
    }
}
