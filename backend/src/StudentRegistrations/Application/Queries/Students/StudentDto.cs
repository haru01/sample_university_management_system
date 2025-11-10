namespace StudentRegistrations.Application.Queries.Students;

public record StudentDto
{
    public required Guid StudentId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required int Grade { get; init; }
}
