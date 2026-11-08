namespace TaskBoard.Api.Models;

public sealed class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int AssignedUserId { get; set; }
    public User AssignedUser { get; set; } = null!;
    public string Status { get; set; } = TaskStatuses.Pending;
    public DateTime CreatedAt { get; set; }
    public string AdditionalData { get; set; } = "{}";
}

public static class TaskStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Done = "Done";

    public static bool IsValid(string? value) => value is Pending or InProgress or Done;
}
