using StudentRegistrations.Domain.Exceptions;
using Shared;
using Shared.ValueObjects;
using System.Text.RegularExpressions;

namespace StudentRegistrations.Domain.StudentAggregate;

/// <summary>
/// 学生集約ルート
/// </summary>
public class Student : AggregateRoot<StudentId>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public int Grade { get; private set; }

    // EF Core用
    private Student() : base(null!)
    {
        Name = string.Empty;
        Email = string.Empty;
    }

    private Student(StudentId id, string name, string email, int grade)
        : base(id)
    {
        Name = name;
        Email = email;
        Grade = grade;
    }

    /// <summary>
    /// 学生を作成
    /// </summary>
    public static Student Create(string name, string email, int grade)
    {
        EnsureNameNotEmpty(name);
        EnsureEmailValid(email);
        EnsureGradeBetween1And4(grade);

        var studentId = StudentId.CreateNew();
        return new Student(studentId, name, email, grade);
    }

    /// <summary>
    /// 学生情報を更新
    /// </summary>
    public void Update(string name, string email, int grade)
    {
        EnsureNameNotEmpty(name);
        EnsureEmailValid(email);
        EnsureGradeBetween1And4(grade);

        Name = name;
        Email = email;
        Grade = grade;
    }

    /// <summary>
    /// 学生名が空でないことを保証
    /// </summary>
    private static void EnsureNameNotEmpty(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("STUDENT_NAME_EMPTY", "Student name cannot be empty");
    }

    /// <summary>
    /// メールアドレスが有効なメール形式であることを保証
    /// </summary>
    private static void EnsureEmailValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("STUDENT_EMAIL_EMPTY", "Email cannot be empty");

        // シンプルなメール形式検証
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
            throw new ValidationException("INVALID_EMAIL_FORMAT", "Invalid email format");
    }

    /// <summary>
    /// 学年が1から4の範囲内であることを保証
    /// </summary>
    private static void EnsureGradeBetween1And4(int grade)
    {
        if (grade < 1 || grade > 4)
            throw new ValidationException("INVALID_GRADE", "Grade must be between 1 and 4");
    }
}
