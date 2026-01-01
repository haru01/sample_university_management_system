using Enrollments.Application.Queries.Enrollments;
using Enrollments.Application.Services;
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
    private readonly IStudentServiceClient _studentServiceClient;

    public GetStudentEnrollmentsQueryHandler(
        IEnrollmentRepository enrollmentRepository,
        ICourseOfferingRepository courseOfferingRepository,
        ICourseRepository courseRepository,
        IStudentServiceClient studentServiceClient)
    {
        _enrollmentRepository = enrollmentRepository;
        _courseOfferingRepository = courseOfferingRepository;
        _courseRepository = courseRepository;
        _studentServiceClient = studentServiceClient;
    }

    public async Task<List<EnrollmentDto>> Handle(GetStudentEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        // 学生の存在確認と名前の取得（StudentRegistrations APIを呼び出す）
        var studentId = new StudentId(request.StudentId);
        var studentName = await _studentServiceClient.GetStudentNameAsync(studentId, cancellationToken);

        if (studentName == null)
        {
            throw new NotFoundException($"学生ID {request.StudentId} が見つかりません");
        }

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

        if (enrollments.Count == 0)
        {
            return new List<EnrollmentDto>();
        }

        // コース開講を一括取得（N+1問題を回避）
        var offeringIds = enrollments.Select(e => e.OfferingId).Distinct().ToList();
        var courseOfferings = await _courseOfferingRepository.GetByIdsAsync(offeringIds, cancellationToken);
        var offeringDict = courseOfferings.ToDictionary(co => co.Id);

        // コース情報を一括取得（N+1問題を回避）
        var courseCodes = courseOfferings.Select(co => co.CourseCode).Distinct().ToList();
        var courses = await _courseRepository.GetByCodesAsync(courseCodes, cancellationToken);
        var courseDict = courses.ToDictionary(c => c.Id);

        // コース開講とコース詳細を含むDTOにマッピング
        var enrollmentDtos = new List<EnrollmentDto>();

        foreach (var enrollment in enrollments)
        {
            if (!offeringDict.TryGetValue(enrollment.OfferingId, out var courseOffering))
            {
                continue; // コース開講が見つからない場合はスキップ
            }

            if (!courseDict.TryGetValue(courseOffering.CourseCode, out var course))
            {
                continue; // コースが見つからない場合はスキップ
            }

            enrollmentDtos.Add(new EnrollmentDto
            {
                EnrollmentId = enrollment.Id.Value,
                StudentId = enrollment.StudentId.Value,
                StudentName = studentName,
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
