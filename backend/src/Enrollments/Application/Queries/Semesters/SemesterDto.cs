namespace Enrollments.Application.Queries.Semesters;

public record SemesterDto
{
    public required int Year { get; init; }
    public required string Period { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
}
