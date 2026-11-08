namespace TaskBoard.Api.Data;

public static class SqlJson
{
    // Translation only: execution in C# is deliberately impossible.
    public static string? Value(string json, string path) => throw new NotSupportedException("Use JSON_VALUE in a SQL query.");
}
