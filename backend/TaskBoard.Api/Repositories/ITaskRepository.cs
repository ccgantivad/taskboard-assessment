using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public interface ITaskRepository
{
    Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken);
    Task<bool> AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task<TaskItem?> FindAsync(int id, CancellationToken cancellationToken);
    Task<bool> SaveStatusAsync(TaskItem task, CancellationToken cancellationToken);
    Task<TaskDto[]> ListAsync(TaskFilters filters, CancellationToken cancellationToken);
}
