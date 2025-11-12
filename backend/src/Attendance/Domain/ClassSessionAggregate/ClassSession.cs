using Shared;

namespace Attendance.Domain.ClassSessionAggregate;

/// <summary>
/// 授業セッション集約ルート
/// コース開講に対する個別の授業回を表す
/// </summary>
public class ClassSession : AggregateRoot<SessionId>
{
    public int OfferingId { get; private set; }
    public int SessionNumber { get; private set; }
    public DateOnly SessionDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string? Location { get; private set; }
    public string? Topic { get; private set; }
    public SessionStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }

    // EF Core用
    private ClassSession() : base(null!)
    {
    }

    private ClassSession(
        SessionId sessionId,
        int offeringId,
        int sessionNumber,
        DateOnly sessionDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? location,
        string? topic)
        : base(sessionId)
    {
        OfferingId = offeringId;
        SessionNumber = sessionNumber;
        SessionDate = sessionDate;
        StartTime = startTime;
        EndTime = endTime;
        Location = location;
        Topic = topic;
        Status = SessionStatus.Scheduled;
    }

    /// <summary>
    /// 授業セッションを作成（SessionIdは手動で生成）
    /// </summary>
    public static ClassSession Create(
        SessionId sessionId,
        int offeringId,
        int sessionNumber,
        DateOnly sessionDate,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly semesterStartDate,
        DateOnly semesterEndDate,
        string? location,
        string? topic)
    {
        EnsureSessionNumberValid(sessionNumber);
        EnsureTimeRangeValid(startTime, endTime);
        EnsureDateWithinSemester(sessionDate, semesterStartDate, semesterEndDate);
        EnsureLocationLength(location);
        EnsureTopicLength(topic);

        return new ClassSession(
            sessionId,
            offeringId,
            sessionNumber,
            sessionDate,
            startTime,
            endTime,
            location,
            topic);
    }

    /// <summary>
    /// 授業セッション情報を更新
    /// </summary>
    public void Update(
        DateOnly sessionDate,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly semesterStartDate,
        DateOnly semesterEndDate,
        string? location,
        string? topic)
    {
        EnsureNotCompletedOrCancelled();
        EnsureTimeRangeValid(startTime, endTime);
        EnsureDateWithinSemester(sessionDate, semesterStartDate, semesterEndDate);
        EnsureLocationLength(location);
        EnsureTopicLength(topic);

        SessionDate = sessionDate;
        StartTime = startTime;
        EndTime = endTime;
        Location = location;
        Topic = topic;
    }

    /// <summary>
    /// 授業セッションをキャンセル
    /// </summary>
    public void Cancel(string? reason)
    {
        if (Status == SessionStatus.Cancelled)
            throw new InvalidOperationException("Session is already cancelled");

        if (Status == SessionStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed session");

        EnsureCancellationReasonLength(reason);

        Status = SessionStatus.Cancelled;
        CancellationReason = reason;
    }

    /// <summary>
    /// 授業セッションを完了
    /// </summary>
    public void Complete()
    {
        if (Status == SessionStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete cancelled session");

        Status = SessionStatus.Completed;
    }

    private void EnsureNotCompletedOrCancelled()
    {
        if (Status == SessionStatus.Completed)
            throw new InvalidOperationException("Cannot update completed session");

        if (Status == SessionStatus.Cancelled)
            throw new InvalidOperationException("Cannot update cancelled session");
    }

    private static void EnsureSessionNumberValid(int sessionNumber)
    {
        if (sessionNumber < 1)
            throw new ArgumentException("Session number must be greater than 0");
    }

    private static void EnsureTimeRangeValid(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("End time must be after start time");
    }

    private static void EnsureDateWithinSemester(
        DateOnly sessionDate,
        DateOnly semesterStartDate,
        DateOnly semesterEndDate)
    {
        if (sessionDate < semesterStartDate || sessionDate > semesterEndDate)
            throw new InvalidOperationException("Session date must be within semester period");
    }

    private static void EnsureLocationLength(string? location)
    {
        if (location?.Length > 50)
            throw new ArgumentException("Location must not exceed 50 characters");
    }

    private static void EnsureTopicLength(string? topic)
    {
        if (topic?.Length > 200)
            throw new ArgumentException("Topic must not exceed 200 characters");
    }

    private static void EnsureCancellationReasonLength(string? reason)
    {
        if (reason?.Length > 200)
            throw new ArgumentException("Cancellation reason must not exceed 200 characters");
    }
}
