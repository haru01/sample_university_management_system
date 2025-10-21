using System.Net;
using System.Text.Json;
using Enrollments.Domain.Exceptions;

namespace Enrollments.Api.Middleware;

/// <summary>
/// グローバル例外ハンドラーミドルウェア
/// 例外を適切なHTTPステータスコードとレスポンスに変換
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorResponse) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse(validationEx.Code, validationEx.Message)
            ),
            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                new ErrorResponse(conflictEx.Code, conflictEx.Message)
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                new ErrorResponse(notFoundEx.Code, notFoundEx.Message)
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("INVALID_ARGUMENT", argEx.Message)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("INTERNAL_SERVER_ERROR", "An unexpected error occurred")
            )
        };

        // ログ出力
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Client error: {StatusCode} - {Message}", statusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(errorResponse, options);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// エラーレスポンスモデル
/// </summary>
public record ErrorResponse(string Code, string Message);
