namespace Enrollments.Application.Queries.GetCourses;

public record CourseDto
{
    public required string CourseCode { get; init; }
    public required string Name { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
}
