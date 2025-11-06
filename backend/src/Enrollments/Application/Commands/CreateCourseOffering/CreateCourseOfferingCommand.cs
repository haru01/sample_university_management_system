using MediatR;

namespace Enrollments.Application.Commands.CreateCourseOffering;

public record CreateCourseOfferingCommand : IRequest<int>
{
    public required string CourseCode { get; init; }
    public required int Year { get; init; }
    public required string Period { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
    public string? Instructor { get; init; }
}
