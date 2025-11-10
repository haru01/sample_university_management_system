using System.Reflection;
using Enrollments.Api.Middleware;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.SemesterAggregate;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
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
        Description = "大学管理システムAPI - 履修管理・出席管理・成績評価"
    });
});

// Database
// 環境変数からの接続文字列を優先、なければappsettingsから取得
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("CoursesDb");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string not found. " +
        "Set DATABASE_CONNECTION_STRING environment variable or configure appsettings.json");
}

builder.Services.AddDbContext<CoursesDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure()));

// PostgreSQL: DateTimeをUTCとして扱う設定
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Repositories
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ISemesterRepository, SemesterRepository>();
builder.Services.AddScoped<ICourseOfferingRepository, CourseOfferingRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

// MediatR - CommandHandlers/QueryHandlersを自動登録
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.Load("Enrollments.Application"));
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
