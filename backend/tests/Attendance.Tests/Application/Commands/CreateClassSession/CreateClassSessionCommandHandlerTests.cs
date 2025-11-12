using Attendance.Application.Commands.CreateClassSession;
using Attendance.Domain.ClassSessionAggregate;
using Attendance.Infrastructure.Persistence;
using Attendance.Infrastructure.Persistence.Repositories;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Tests.Application.Commands.CreateClassSession;

/// <summary>
/// CreateClassSessionCommandHandlerのテスト
/// US-CS01: 授業セッションを登録できる
/// </summary>
public class CreateClassSessionCommandHandlerTests : IDisposable
{
    private readonly AttendanceDbContext _attendanceContext;
    private readonly CoursesDbContext _coursesContext;
    private readonly CreateClassSessionCommandHandler _handler;
    private readonly ClassSessionRepository _classSessionRepository;
    private readonly CourseOfferingRepository _courseOfferingRepository;
    private readonly SemesterRepository _semesterRepository;

    public CreateClassSessionCommandHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var attendanceOptions = new DbContextOptionsBuilder<AttendanceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var coursesOptions = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _attendanceContext = new AttendanceDbContext(attendanceOptions);
        _coursesContext = new CoursesDbContext(coursesOptions);

        // リポジトリの初期化
        _classSessionRepository = new ClassSessionRepository(_attendanceContext);
        _courseOfferingRepository = new CourseOfferingRepository(_coursesContext);
        _semesterRepository = new SemesterRepository(_coursesContext);

        // ハンドラーの初期化
        _handler = new CreateClassSessionCommandHandler(
            _classSessionRepository,
            _courseOfferingRepository,
            _semesterRepository);
    }

    public void Dispose()
    {
        _attendanceContext?.Dispose();
        _coursesContext?.Dispose();
    }

    [Fact]
    public async Task 有効な授業セッション情報で新しいセッションを登録する()
    {
        // Arrange: CourseOfferingとSemesterを準備
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Location = "A棟201教室",
            Topic = "プログラミング基礎：変数とデータ型"
        };

        // Act
        var sessionId = await _handler.Handle(command, default);

        // Assert
        Assert.True(sessionId > 0);

        var savedSession = await _attendanceContext.ClassSessions
            .FirstOrDefaultAsync(cs => cs.Id.Value == sessionId);
        Assert.NotNull(savedSession);
        Assert.Equal(1, savedSession.SessionNumber);
        Assert.Equal(new DateOnly(2024, 4, 10), savedSession.SessionDate);
        Assert.Equal(SessionStatus.Scheduled, savedSession.Status);
        Assert.Equal("A棟201教室", savedSession.Location);
        Assert.Equal("プログラミング基礎：変数とデータ型", savedSession.Topic);
    }

    [Fact]
    public async Task 存在しないOfferingIdでセッションを登録しようとするとKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var command = new CreateClassSessionCommand
        {
            OfferingId = 999, // 存在しないOfferingId
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("CourseOffering not found", exception.Message);
    }

    [Fact]
    public async Task 同じコース開講で重複するセッション番号で登録を試みるとInvalidOperationExceptionがスローされる()
    {
        // Arrange: 既存のClassSessionを準備
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        // 既存のセッションを登録
        var existingSession = ClassSession.Create(
            1,
            1, // SessionNumber 1
            new DateOnly(2024, 4, 10),
            new TimeOnly(9, 0),
            new TimeOnly(10, 30),
            new DateOnly(2024, 4, 1),
            new DateOnly(2024, 9, 30),
            null,
            null);
        await _attendanceContext.ClassSessions.AddAsync(existingSession);
        await _attendanceContext.SaveChangesAsync();

        // 同じSessionNumberで登録を試みる
        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1, // 既に存在
            SessionDate = new DateOnly(2024, 4, 15),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Session number already exists for this offering", exception.Message);
    }

    [Fact]
    public async Task 開始時刻が終了時刻より後の時刻でセッションを登録しようとするとArgumentExceptionがスローされる()
    {
        // Arrange
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(10, 30), // 終了時刻より後
            EndTime = new TimeOnly(9, 0)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("End time must be after start time", exception.Message);
    }

    [Fact]
    public async Task 学期期間外の日付でセッションを登録しようとするとInvalidOperationExceptionがスローされる()
    {
        // Arrange
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 10, 1), // 学期期間外
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Session date must be within semester period", exception.Message);
    }

    [Fact]
    public async Task Locationが50文字を超える場合はArgumentExceptionがスローされる()
    {
        // Arrange
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Location = new string('A', 51) // 51文字
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Location must not exceed 50 characters", exception.Message);
    }

    [Fact]
    public async Task Topicが200文字を超える場合はArgumentExceptionがスローされる()
    {
        // Arrange
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Topic = new string('T', 201) // 201文字
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Topic must not exceed 200 characters", exception.Message);
    }

    [Fact]
    public async Task LocationとTopicがnullでも登録できる()
    {
        // Arrange
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30),
            Location = null,
            Topic = null
        };

        // Act
        var sessionId = await _handler.Handle(command, default);

        // Assert
        Assert.True(sessionId > 0);

        var savedSession = await _attendanceContext.ClassSessions
            .FirstOrDefaultAsync(cs => cs.Id.Value == sessionId);
        Assert.NotNull(savedSession);
        Assert.Null(savedSession.Location);
        Assert.Null(savedSession.Topic);
    }

    [Fact]
    public async Task 初期ステータスはScheduledである()
    {
        // Arrange
        var semester = Semester.Create(2024, "Spring", new DateTime(2024, 4, 1), new DateTime(2024, 9, 30));
        await _coursesContext.Semesters.AddAsync(semester);
        await _coursesContext.SaveChangesAsync();

        var courseOffering = CourseOffering.Create(
            new OfferingId(1),
            new CourseCode("CS101"),
            semester.Id,
            3,
            50,
            "田中太郎");
        _coursesContext.CourseOfferings.Add(courseOffering);
        await _coursesContext.SaveChangesAsync();

        var command = new CreateClassSessionCommand
        {
            OfferingId = 1,
            SessionNumber = 1,
            SessionDate = new DateOnly(2024, 4, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 30)
        };

        // Act
        var sessionId = await _handler.Handle(command, default);

        // Assert
        var savedSession = await _attendanceContext.ClassSessions
            .FirstOrDefaultAsync(cs => cs.Id.Value == sessionId);
        Assert.NotNull(savedSession);
        Assert.Equal(SessionStatus.Scheduled, savedSession.Status);
    }
}
