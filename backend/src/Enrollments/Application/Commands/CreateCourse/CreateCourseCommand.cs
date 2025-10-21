namespace Enrollments.Application.Commands.CreateCourse;

public record CreateCourseCommand
{
    public required string CourseCode { get; init; }
    public required string Name { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
}
