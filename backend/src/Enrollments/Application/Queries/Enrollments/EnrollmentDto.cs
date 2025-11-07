namespace Enrollments.Application.Queries.Enrollments;

/// <summary>
/// 履修登録データのDTO
/// </summary>
public record EnrollmentDto
{
    public required Guid EnrollmentId { get; init; }
    public required Guid StudentId { get; init; }
    public required string StudentName { get; init; }
    public required int OfferingId { get; init; }
    public required string CourseCode { get; init; }
    public required string CourseName { get; init; }
    public required int Year { get; init; }
    public required string Period { get; init; }
    public required int Credits { get; init; }
    public string? Instructor { get; init; }
    public required string Status { get; init; }
    public required DateTime EnrolledAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
}
