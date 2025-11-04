using Enrollments.Application.Queries.Semesters;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetCurrentSemester;

/// <summary>
/// 現在の学期取得クエリハンドラー
/// </summary>
public class GetCurrentSemesterQueryHandler : IRequestHandler<GetCurrentSemesterQuery, SemesterDto?>
{
    private readonly ISemesterRepository _semesterRepository;

    public GetCurrentSemesterQueryHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<SemesterDto?> Handle(GetCurrentSemesterQuery request, CancellationToken cancellationToken)
    {
        var semester = await _semesterRepository.GetCurrentSemesterAsync(cancellationToken);

        if (semester is null)
        {
            return null;
        }

        return new SemesterDto
        {
            Year = semester.Id.Year,
            Period = semester.Id.Period,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate
        };
    }
}
