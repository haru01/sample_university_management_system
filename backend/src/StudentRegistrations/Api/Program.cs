using System.Reflection;
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
        Title = "Student Registrations API",
        Version = "v1",
        Description = "学生在籍管理システムAPI - 学生情報管理"
    });
});

// Database
// 環境変数からの接続文字列を優先、なければappsettingsから取得
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("StudentRegistrationsDb");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string not found. " +
        "Set DATABASE_CONNECTION_STRING environment variable or configure appsettings.json");
}

builder.Services.AddDbContext<StudentRegistrationsDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure()));

// PostgreSQL: DateTimeをUTCとして扱う設定
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Repositories
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

// MediatR - CommandHandlers/QueryHandlersを自動登録
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.Load("StudentRegistrations.Application"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Registrations API v1");
    c.RoutePrefix = string.Empty; // Swagger UIをルートパス(/)で表示
});

// 認証・認可は後で実装予定
// app.UseAuthorization();
app.MapControllers();

app.Run();
