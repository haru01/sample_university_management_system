using Enrollments.Domain.SemesterAggregate;
using Enrollments.Domain.Exceptions;
using MediatR;

namespace Enrollments.Application.Commands.CreateSemester;

/// <summary>
/// 学期作成コマンドハンドラー
/// </summary>
public class CreateSemesterCommandHandler : IRequestHandler<CreateSemesterCommand, SemesterId>
{
    private readonly ISemesterRepository _semesterRepository;

    public CreateSemesterCommandHandler(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<SemesterId> Handle(CreateSemesterCommand request, CancellationToken cancellationToken)
    {
        // 学期IDの作成と重複チェック
        var semesterId = new SemesterId(request.Year, request.Period);
        var existing = await _semesterRepository.GetByIdAsync(semesterId, cancellationToken);
        if (existing != null)
            throw new ConflictException("SEMESTER_ALREADY_EXISTS", "Semester already exists");

        // 学期を作成
        var semester = Semester.Create(
            request.Year,
            request.Period,
            request.StartDate,
            request.EndDate);

        // 永続化
        await _semesterRepository.AddAsync(semester, cancellationToken);
        await _semesterRepository.SaveChangesAsync(cancellationToken);

        return semester.Id;
    }
}
