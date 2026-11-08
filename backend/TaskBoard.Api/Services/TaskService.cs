using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;

namespace TaskBoard.Api.Services;

public sealed class TaskService(ITaskRepository repository)
{
    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (!await repository.UserExistsAsync(request.AssignedUserId, cancellationToken))
            throw new ApiProblemException(400, "The assigned user does not exist.");
        var task = new TaskItem
        {
            Title = request.Title!, AssignedUserId = request.AssignedUserId,
            Status = TaskStatuses.Pending, AdditionalData = request.AdditionalData.GetRawText()
        };
        if (!await repository.AddAsync(task, cancellationToken))
            throw new ApiProblemException(400, "The assigned user is no longer available.");
        var stored = await repository.FindAsync(task.Id, cancellationToken)
            ?? throw new ApiProblemException(409, "The task changed before it could be returned. Refresh and try again.");
        return ToDto(stored);
    }

    public Task<TaskDto[]> ListAsync(TaskFilters filters, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(filters, new ValidationContext(filters), true);
        return repository.ListAsync(filters, cancellationToken);
    }

    public async Task<TaskDto> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var task = await repository.FindAsync(id, cancellationToken)
            ?? throw new ApiProblemException(404, "The task does not exist.");
        if (task.Status == TaskStatuses.Pending && request.Status == TaskStatuses.Done)
            throw new ApiProblemException(409, "A Pending task cannot transition directly to Done.");
        task.Status = request.Status!;
        if (!await repository.SaveStatusAsync(task, cancellationToken))
            throw new ApiProblemException(409, "The task was changed concurrently. Refresh and try again.");
        return ToDto(task);
    }

    private static TaskDto ToDto(TaskItem task) => new(task.Id, task.Title, task.Status,
        task.AssignedUserId, task.AssignedUser.Name, DateTime.SpecifyKind(task.CreatedAt, DateTimeKind.Utc),
        JsonSerializer.Deserialize<JsonElement>(task.AdditionalData));
}
