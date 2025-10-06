namespace CodeChavez.M3diator;

public class Result<T>
{
    public virtual T Value { get; private set; }
    public virtual bool IsSuccess { get; private set; } = true;
    public virtual string ErrorMessage { get; private set; } = string.Empty;

    public static Result<T> Success() => new();
    public static Result<T> Success(T data) => new() { Value = data };

    public static Result<T> Fail(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
    public static Result<T> Fail() => new Result<T>()
    {
        ErrorMessage = string.Empty,
        IsSuccess = false
    };
}
