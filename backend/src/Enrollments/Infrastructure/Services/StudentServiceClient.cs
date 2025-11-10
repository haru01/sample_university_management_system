using System.Net.Http.Json;
using Enrollments.Application.Services;
using Shared.ValueObjects;

namespace Enrollments.Infrastructure.Services;

/// <summary>
/// StudentRegistrationsコンテキストへのAnti-Corruption Layer実装（HTTP経由）
/// </summary>
/// <remarks>
/// StudentRegistrations APIのREST APIを呼び出して学生情報を取得します。
/// これにより、EnrollmentsコンテキストとStudentRegistrationsコンテキストが
/// 疎結合に保たれ、それぞれ独立したデプロイが可能になります。
/// </remarks>
public class StudentServiceClient : IStudentServiceClient
{
    private readonly HttpClient _httpClient;

    public StudentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // StudentRegistrations APIのGET /api/students/{studentId}エンドポイントを呼び出す
            var response = await _httpClient.GetAsync(
                $"api/students/{studentId.Value}",
                cancellationToken);

            // 200 OKなら存在する、404 Not Foundなら存在しない
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException)
        {
            // HTTP通信エラーの場合はfalseを返す
            // TODO: ロギングやリトライ処理を追加する
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetStudentNameAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // StudentRegistrations APIのGET /api/students/{studentId}エンドポイントを呼び出す
            var response = await _httpClient.GetAsync(
                $"api/students/{studentId.Value}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            // レスポンスから学生情報を取得
            var studentDto = await response.Content.ReadFromJsonAsync<StudentDto>(cancellationToken);
            return studentDto?.Name;
        }
        catch (HttpRequestException)
        {
            // HTTP通信エラーの場合はnullを返す
            // TODO: ロギングやリトライ処理を追加する
            return null;
        }
    }

    /// <summary>
    /// StudentRegistrations APIのレスポンスDTO
    /// </summary>
    private class StudentDto
    {
        public Guid StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Grade { get; set; }
    }
}
