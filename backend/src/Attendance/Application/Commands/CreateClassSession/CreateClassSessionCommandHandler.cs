using Attendance.Domain.ClassSessionAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Attendance.Application.Commands.CreateClassSession;

/// <summary>
/// 授業セッション作成コマンドハンドラー
/// </summary>
public class CreateClassSessionCommandHandler : IRequestHandler<CreateClassSessionCommand, int>
{
    private readonly IClassSessionRepository _classSessionRepository;
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ISemesterRepository _semesterRepository;

    public CreateClassSessionCommandHandler(
        IClassSessionRepository classSessionRepository,
        ICourseOfferingRepository courseOfferingRepository,
        ISemesterRepository semesterRepository)
    {
        _classSessionRepository = classSessionRepository;
        _courseOfferingRepository = courseOfferingRepository;
        _semesterRepository = semesterRepository;
    }

    public async Task<int> Handle(CreateClassSessionCommand request, CancellationToken cancellationToken)
    {
        // CourseOfferingの存在確認
        var offeringId = new OfferingId(request.OfferingId);
        var courseOffering = await _courseOfferingRepository.GetByIdAsync(offeringId, cancellationToken);
        if (courseOffering == null)
            throw new KeyNotFoundException("CourseOffering not found");

        // CourseOfferingがActiveであることを確認
        if (courseOffering.Status != OfferingStatus.Active)
            throw new InvalidOperationException("CourseOffering is not active");

        // Semesterの情報を取得して学期期間を確認
        var semester = await _semesterRepository.GetByIdAsync(courseOffering.SemesterId, cancellationToken);
        if (semester == null)
            throw new KeyNotFoundException("Semester not found");

        // 同じOfferingIdとSessionNumberの組み合わせが既に存在しないことを確認
        var existingSession = await _classSessionRepository.GetByOfferingAndSessionNumberAsync(
            request.OfferingId,
            request.SessionNumber,
            cancellationToken);
        if (existingSession != null)
            throw new InvalidOperationException("Session number already exists for this offering");

        // 授業セッションを作成
        var classSession = ClassSession.Create(
            request.OfferingId,
            request.SessionNumber,
            request.SessionDate,
            request.StartTime,
            request.EndTime,
            DateOnly.FromDateTime(semester.StartDate),
            DateOnly.FromDateTime(semester.EndDate),
            request.Location,
            request.Topic);

        // 永続化
        await _classSessionRepository.AddAsync(classSession, cancellationToken);
        await _classSessionRepository.SaveChangesAsync(cancellationToken);

        // SaveChanges後、データベースで生成されたIDを取得
        // OfferingIdとSessionNumberで一意に特定できるため、再検索する
        var savedSession = await _classSessionRepository.GetByOfferingAndSessionNumberAsync(
            request.OfferingId,
            request.SessionNumber,
            cancellationToken);

        if (savedSession == null)
            throw new InvalidOperationException("Failed to retrieve saved session");

        return savedSession.Id.Value;
    }
}
