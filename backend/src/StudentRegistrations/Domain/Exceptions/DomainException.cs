namespace StudentRegistrations.Domain.Exceptions;

/// <summary>
/// ドメイン層の基底例外
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    protected DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// バリデーションエラー（400 Bad Request）
/// </summary>
public class ValidationException : DomainException
{
    public ValidationException(string message)
        : base("VALIDATION_ERROR", message)
    {
    }

    public ValidationException(string code, string message)
        : base(code, message)
    {
    }
}

/// <summary>
/// リソース重複エラー（409 Conflict）
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base("CONFLICT", message)
    {
    }

    public ConflictException(string code, string message)
        : base(code, message)
    {
    }
}

/// <summary>
/// リソース未検出エラー（404 Not Found）
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message)
        : base("NOT_FOUND", message)
    {
    }

    public NotFoundException(string code, string message)
        : base(code, message)
    {
    }
}
