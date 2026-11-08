using System.Text.Json;

namespace TaskBoard.Api.DTOs;

public sealed record TaskDto(int Id, string Title, string Status, int AssignedUserId,
    string AssignedUserName, DateTime CreatedAt, JsonElement AdditionalData);
