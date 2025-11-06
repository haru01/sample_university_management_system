using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.SemesterAggregate;
using MediatR;

namespace Enrollments.Application.Commands.CreateCourseOffering;

/// <summary>
/// コース開講作成コマンドハンドラー
/// </summary>
public class CreateCourseOfferingCommandHandler : IRequestHandler<CreateCourseOfferingCommand, int>
{
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ISemesterRepository _semesterRepository;

    public CreateCourseOfferingCommandHandler(
        ICourseOfferingRepository courseOfferingRepository,
        ICourseRepository courseRepository,
        ISemesterRepository semesterRepository)
    {
        _courseOfferingRepository = courseOfferingRepository;
        _courseRepository = courseRepository;
        _semesterRepository = semesterRepository;
    }

    public async Task<int> Handle(CreateCourseOfferingCommand request, CancellationToken cancellationToken)
    {
        // 値オブジェクト構築
        var courseCode = new CourseCode(request.CourseCode);
        var semesterId = new SemesterId(request.Year, request.Period);

        // コースマスタの存在確認
        var course = await _courseRepository.GetByCodeAsync(courseCode, cancellationToken);
        if (course == null)
            throw new KeyNotFoundException($"Course not found: {courseCode}");

        // 学期の存在確認
        var semester = await _semesterRepository.GetByIdAsync(semesterId, cancellationToken);
        if (semester == null)
            throw new KeyNotFoundException($"Semester not found: {semesterId}");

        // 同一学期に既に開講されていないか確認
        var existing = await _courseOfferingRepository.GetByCourseAndSemesterAsync(
            courseCode, semesterId, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException(
                $"Course {courseCode} is already offered in this semester {semesterId}");

        // 次のOfferingIdを取得
        var offeringId = await _courseOfferingRepository.GetNextOfferingIdAsync(cancellationToken);

        // コース開講作成
        var courseOffering = CourseOffering.Create(
            offeringId,
            courseCode,
            semesterId,
            request.Credits,
            request.MaxCapacity,
            request.Instructor);

        // 永続化
        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync(cancellationToken);

        return courseOffering.Id.Value;
    }
}
