namespace Focus.Application.Common;

public enum Failure
{
    Invalid,
    Unauthenticated,
    Forbidden,
    NotFound,
    Conflict,
    Unprocessable
}

public sealed class UseCaseException(Failure failure, string message) : Exception(message)
{
    public Failure Failure { get; } = failure;

    public static UseCaseException NotFound() => new(Failure.NotFound, "Resource not found.");

    public static UseCaseException Conflict(string message) => new(Failure.Conflict, message);
}
