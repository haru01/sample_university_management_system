using Enrollments.Application.Queries.CourseOfferings;
using MediatR;

namespace Enrollments.Application.Queries.GetCourseOffering;

public record GetCourseOfferingQuery : IRequest<CourseOfferingDto>
{
    public required int OfferingId { get; init; }
}
