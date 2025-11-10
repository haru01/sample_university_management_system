using Enrollments.Application.Queries.Enrollments;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.SemesterAggregate;
using MediatR;
using Shared.ValueObjects;

namespace Enrollments.Application.Queries.GetStudentEnrollments;

/// <summary>
/// 学生の履修登録一覧を取得するハンドラー
/// </summary>
public class GetStudentEnrollmentsQueryHandler : IRequestHandler<GetStudentEnrollmentsQuery, List<EnrollmentDto>>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseOfferingRepository _courseOfferingRepository;
    private readonly ICourseRepository _courseRepository;

    public GetStudentEnrollmentsQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseOfferingRepository courseOfferingRepository,
        ICourseRepository courseRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseOfferingRepository = courseOfferingRepository;
        _courseRepository = courseRepository;
    }

    public async Task<List<EnrollmentDto>> Handle(GetStudentEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        // TODO: Phase 8でStudentRegistrations APIを呼び出して学生情報（名前など）を取得する
        var studentId = new StudentId(request.StudentId);

        // ステータスフィルターをパース（指定されている場合）
        EnrollmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.StatusFilter))
        {
            if (Enum.TryParse<EnrollmentStatus>(request.StatusFilter, true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }
            else
            {
                throw new ValidationException($"無効なステータスフィルター: {request.StatusFilter}。有効な値: Enrolled, Completed, Cancelled");
            }
        }

        // 履修登録を取得
        var enrollments = await _enrollmentRepository.SelectByStudentAsync(
            studentId, statusFilter, cancellationToken);

        // コース開講とコース詳細を含むDTOにマッピング
        var enrollmentDtos = new List<EnrollmentDto>();

        foreach (var enrollment in enrollments)
        {
            var courseOffering = await _courseOfferingRepository.GetByIdAsync(
                enrollment.OfferingId, cancellationToken);

            if (courseOffering == null)
            {
                continue; // コース開講が見つからない場合はスキップ
            }

            var course = await _courseRepository.GetByCodeAsync(
                courseOffering.CourseCode, cancellationToken);

            if (course == null)
            {
                continue; // コースが見つからない場合はスキップ
            }

            enrollmentDtos.Add(new EnrollmentDto
            {
                EnrollmentId = enrollment.Id.Value,
                StudentId = enrollment.StudentId.Value,
                StudentName = "", // TODO: Phase 8でStudentRegistrations APIから学生名を取得
                OfferingId = enrollment.OfferingId.Value,
                CourseCode = courseOffering.CourseCode.Value,
                CourseName = course.Name,
                Year = courseOffering.SemesterId.Year,
                Period = courseOffering.SemesterId.Period,
                Credits = courseOffering.Credits,
                Instructor = courseOffering.Instructor,
                Status = enrollment.Status.ToString(),
                EnrolledAt = enrollment.EnrolledAt,
                CompletedAt = enrollment.CompletedAt,
                CancelledAt = enrollment.CancelledAt
            });
        }

        return enrollmentDtos;
    }
}
