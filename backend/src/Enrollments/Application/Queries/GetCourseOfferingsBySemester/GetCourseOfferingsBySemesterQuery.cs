using Enrollments.Application.Queries.CourseOfferings;
using MediatR;

namespace Enrollments.Application.Queries.GetCourseOfferingsBySemester;

public record GetCourseOfferingsBySemesterQuery : IRequest<List<CourseOfferingDto>>
{
    public required int Year { get; init; }
    public required string Period { get; init; }
    public string? StatusFilter { get; init; }
}
