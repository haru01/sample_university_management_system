using Enrollments.Application.Services;
using Shared.ValueObjects;
using StudentRegistrations.Domain.StudentAggregate;

namespace Api.Services;

/// <summary>
/// 統合API内で使用する、リポジトリ経由の直接アクセス実装
/// HTTP経由のACLは不要（同一プロセス内）
/// </summary>
public class DirectStudentServiceClient : IStudentServiceClient
{
    private readonly IStudentRepository _studentRepository;

    public DirectStudentServiceClient(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<bool> ExistsAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        return student != null;
    }

    public async Task<string?> GetStudentNameAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        return student?.Name;
    }
}
