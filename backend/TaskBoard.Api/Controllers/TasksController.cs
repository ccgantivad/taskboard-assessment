using Microsoft.AspNetCore.Mvc;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Services;

namespace TaskBoard.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController(TaskService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(CreateTaskRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request, cancellationToken));

    [HttpGet]
    public async Task<ActionResult<TaskDto[]>> List([FromQuery] TaskFilters filters, CancellationToken cancellationToken)
    {
        foreach (var name in new[] { "userId", "status", "priority" })
            if (Request.Query.TryGetValue(name, out var values) &&
                (values.Count != 1 || string.IsNullOrWhiteSpace(values[0])))
                throw new ApiProblemException(400, $"The {name} filter must contain one valid value.");
        return Ok(await service.ListAsync(filters, cancellationToken));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TaskDto>> UpdateStatus(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateStatusAsync(id, request, cancellationToken));
}
