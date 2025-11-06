namespace Enrollments.Application.Queries.CourseOfferings;

public record CourseOfferingDto
{
    public required int OfferingId { get; init; }
    public required string CourseCode { get; init; }
    public required string CourseName { get; init; }
    public required int Year { get; init; }
    public required string Period { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
    public string? Instructor { get; init; }
    public required string Status { get; init; }
}
