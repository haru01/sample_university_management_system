using Enrollments.Application.Queries.Semesters;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Enrollments.Application.Queries.SelectSemesters;

/// <summary>
/// 学期一覧取得クエリハンドラー
/// </summary>
public class SelectSemestersQueryHandler : IRequestHandler<SelectSemestersQuery, List<SemesterDto>>
{
    private readonly ISemesterRepository _semesterRepository;

    public SelectSemestersQueryHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<List<SemesterDto>> Handle(SelectSemestersQuery request, CancellationToken cancellationToken)
    {
        var semesters = await _semesterRepository.SelectAllAsync(cancellationToken);

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
