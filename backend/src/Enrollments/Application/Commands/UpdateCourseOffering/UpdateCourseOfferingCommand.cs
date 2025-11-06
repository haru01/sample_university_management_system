using MediatR;

namespace Enrollments.Application.Commands.UpdateCourseOffering;

public record UpdateCourseOfferingCommand : IRequest<Unit>
{
    public required int OfferingId { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
    public string? Instructor { get; init; }
}
