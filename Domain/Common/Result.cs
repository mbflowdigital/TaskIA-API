namespace Domain.Common;

/// <summary>
/// Result pattern para evitar exceções no fluxo
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public IEnumerable<string> Errors { get; }

    protected Result(bool isSuccess, string message, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result Success(string message = "Operação realizada com sucesso")
        => new(true, message);

    public static Result Failure(string message, IEnumerable<string>? errors = null)
        => new(false, message, errors);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, string message, T? data, IEnumerable<string>? errors = null)
        : base(isSuccess, message, errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data, string message = "Operação realizada com sucesso")
        => new(true, message, data);

    public static new Result<T> Failure(string message, IEnumerable<string>? errors = null)
        => new(false, message, default, errors);
}
