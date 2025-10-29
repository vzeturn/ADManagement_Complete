namespace ADManagement.Domain.Common;

/// <summary>
/// Represents the result of an operation
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public string Message { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = new();
    
    protected Result(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }
    
    protected Result(bool isSuccess, string message, List<string> errors)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors;
    }
    
    public static Result Success(string message = "Operation completed successfully")
        => new(true, message);
    
    public static Result Failure(string message, List<string>? errors = null)
        => new(false, message, errors ?? new List<string> { message });
    
    public static Result<T> Success<T>(T value, string message = "Operation completed successfully")
        => new(true, value, message);
    
    public static Result<T> Failure<T>(string message, List<string>? errors = null)
        => new(false, default!, message, errors ?? new List<string> { message });
}

/// <summary>
/// Represents the result of an operation with a return value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }
    
    internal Result(bool isSuccess, T value, string message) : base(isSuccess, message)
    {
        Value = value;
    }
    
    internal Result(bool isSuccess, T value, string message, List<string> errors) 
        : base(isSuccess, message, errors)
    {
        Value = value;
    }
}
