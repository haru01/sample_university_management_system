using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.StudentAggregate;
using MediatR;
using Shared.ValueObjects;

namespace Enrollments.Application.Commands.EnrollStudent;

/// <summary>
/// 学生をコース開講に履修登録するハンドラー
/// </summary>
public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, Guid>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseOfferingRepository _courseOfferingRepository;

    public EnrollStudentCommandHandler(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ICourseOfferingRepository courseOfferingRepository)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _courseOfferingRepository = courseOfferingRepository;
    }

    public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        // 1. 学生が存在するか検証
        var studentId = new StudentId(request.StudentId);
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student == null)
        {
            throw new NotFoundException($"学生ID {request.StudentId} が見つかりません");
        }

        // 2. コース開講が存在し、アクティブか検証
        var offeringId = new OfferingId(request.OfferingId);
        var courseOffering = await _courseOfferingRepository.GetByIdAsync(offeringId, cancellationToken);
        if (courseOffering == null)
        {
            throw new NotFoundException($"コース開講ID {request.OfferingId} が見つかりません");
        }

        if (courseOffering.Status != OfferingStatus.Active)
        {
            throw new ValidationException("キャンセル済みのコース開講には履修登録できません");
        }

        // 3. 学生が既にこのコース開講に履修登録しているか確認
        var existingEnrollment = await _enrollmentRepository.GetByStudentAndOfferingAsync(
            studentId, offeringId, cancellationToken);

        if (existingEnrollment != null && existingEnrollment.IsActive())
        {
            throw new ConflictException("学生は既にこのコース開講に履修登録しています");
        }

        // 4. コース開講が定員に達しているか確認
        var currentEnrollmentCount = await _enrollmentRepository.CountActiveEnrollmentsByOfferingAsync(
            offeringId, cancellationToken);

        if (currentEnrollmentCount >= courseOffering.MaxCapacity)
        {
            throw new ConflictException("コース開講は定員に達しています");
        }

        // 5. 履修登録を作成
        var enrollment = Enrollment.Create(
            studentId,
            offeringId,
            request.EnrolledBy,
            request.InitialNote);

        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);

        return enrollment.Id.Value;
    }
}
