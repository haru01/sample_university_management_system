using Enrollments.Domain.StudentAggregate;

namespace Enrollments.Tests.Builders;

/// <summary>
/// テストデータビルダー - Student
/// </summary>
public class StudentBuilder
{
    private StudentId _id = new();
    private string _name = "デフォルト名前です";
    private string _email = "default@example.com";
    private int _grade = 1;

    public StudentBuilder WithId(Guid id)
    {
        _id = new StudentId(id);
        return this;
    }

    public StudentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public StudentBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public StudentBuilder WithGrade(int grade)
    {
        _grade = grade;
        return this;
    }

    public Student Build() => Student.Create(_name, _email, _grade);
}
