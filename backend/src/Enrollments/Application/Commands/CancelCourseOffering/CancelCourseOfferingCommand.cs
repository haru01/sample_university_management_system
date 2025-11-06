using MediatR;

namespace Enrollments.Application.Commands.CancelCourseOffering;

public record CancelCourseOfferingCommand : IRequest<Unit>
{
    public required int OfferingId { get; init; }
}
