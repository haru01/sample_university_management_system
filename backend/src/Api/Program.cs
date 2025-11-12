using System.Reflection;
using Api.Middleware;
using Api.Services;
using Attendance.Domain.ClassSessionAggregate;
using Attendance.Infrastructure.Persistence;
using Attendance.Infrastructure.Persistence.Repositories;
using Enrollments.Application.Services;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.SemesterAggregate;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using StudentRegistrations.Domain.StudentAggregate;
using StudentRegistrations.Infrastructure.Persistence;
using StudentRegistrations.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "University Management API",
        Version = "v1",
        Description = "大学管理システム統合API - 学生在籍管理・履修管理"
    });
});

// Database - StudentRegistrations
var studentRegistrationsConnectionString =
    builder.Configuration.GetConnectionString("StudentRegistrationsDb");

if (string.IsNullOrEmpty(studentRegistrationsConnectionString))
{
    throw new InvalidOperationException(
        "StudentRegistrations connection string not found. " +
        "Configure ConnectionStrings:StudentRegistrationsDb in appsettings.json or environment variables");
}

builder.Services.AddDbContext<StudentRegistrationsDbContext>(options =>
    options.UseNpgsql(studentRegistrationsConnectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure()));

// Database - Courses (Enrollments)
var coursesConnectionString =
    builder.Configuration.GetConnectionString("CoursesDb");

if (string.IsNullOrEmpty(coursesConnectionString))
{
    throw new InvalidOperationException(
        "Courses connection string not found. " +
        "Configure ConnectionStrings:CoursesDb in appsettings.json or environment variables");
}

builder.Services.AddDbContext<CoursesDbContext>(options =>
    options.UseNpgsql(coursesConnectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure()));

// Database - Attendance (同じCoursesデータベースを使用)
builder.Services.AddDbContext<AttendanceDbContext>(options =>
    options.UseNpgsql(coursesConnectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure()));

// PostgreSQL: DateTimeをUTCとして扱う設定
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Repositories - StudentRegistrations
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

// Repositories - Enrollments
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ICourseOfferingRepository, CourseOfferingRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

// Repositories - Attendance
builder.Services.AddScoped<IClassSessionRepository, ClassSessionRepository>();

// Student Service - 統合API内でリポジトリ経由で直接アクセス
// HTTP経由のACLは不要（同一プロセス内）
builder.Services.AddScoped<IStudentServiceClient, DirectStudentServiceClient>();

// MediatR - CommandHandlers/QueryHandlersを自動登録
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.Load("StudentRegistrations.Application"));
    cfg.RegisterServicesFromAssembly(Assembly.Load("Enrollments.Application"));
    cfg.RegisterServicesFromAssembly(Assembly.Load("Attendance.Application"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// グローバル例外ハンドラー（最初に登録）
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "University Management API v1");
    c.RoutePrefix = string.Empty; // Swagger UIをルートパス(/)で表示
});

// 認証・認可は後で実装予定
// app.UseAuthorization();
app.MapControllers();

app.Run();
