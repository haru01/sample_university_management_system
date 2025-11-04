using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.Exceptions;
using MediatR;

namespace Enrollments.Application.Commands.CreateCourse;

/// <summary>
/// コース作成コマンドハンドラー
/// </summary>
public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, string>
{
    private readonly ICourseRepository _courseRepository;

    public CreateCourseCommandHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<string> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // 値オブジェクト構築
        var courseCode = new CourseCode(request.CourseCode);

        // 重複チェック
        var existing = await _courseRepository.GetByCodeAsync(courseCode, cancellationToken);
        if (existing != null)
            throw new ConflictException("COURSE_ALREADY_EXISTS", $"Course with code {courseCode} already exists");

        // コース作成
        var course = Course.Create(
            courseCode,
            request.Name,
            request.Credits,
            request.MaxCapacity);

        // 永続化
        await _courseRepository.AddAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return course.Id.Value;
    }
}
