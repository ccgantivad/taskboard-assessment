namespace TaskBoard.Api.Services;

public sealed class ApiProblemException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
