using Enrollments.Domain.SemesterAggregate;
using Enrollments.Domain.Exceptions;

namespace Enrollments.Application.Commands.CreateSemester;

/// <summary>
/// 学期作成サービス実装
/// </summary>
public class CreateSemesterService : ICreateSemesterService
{
    private readonly ISemesterRepository _semesterRepository;

    public CreateSemesterService(ISemesterRepository semesterRepository)
    {
        _semesterRepository = semesterRepository;
    }

    public async Task<SemesterId> CreateSemesterAsync(CreateSemesterCommand command)
    {
        // 学期IDの作成と重複チェック
        var semesterId = new SemesterId(command.Year, command.Period);
        var existing = await _semesterRepository.GetByIdAsync(semesterId);
        if (existing != null)
            throw new ConflictException("SEMESTER_ALREADY_EXISTS", "Semester already exists");

        // 学期を作成
        var semester = Semester.Create(
            command.Year,
            command.Period,
            command.StartDate,
            command.EndDate);

        // 永続化
        await _semesterRepository.AddAsync(semester);
        await _semesterRepository.SaveChangesAsync();

        return semester.Id;
    }
}
