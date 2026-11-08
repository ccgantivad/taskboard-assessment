using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public sealed class TaskRepository(TaskBoardDbContext db) : ITaskRepository
{
    public Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.Id == userId, cancellationToken);

    public async Task<bool> AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        db.Tasks.Add(task);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 547 } sql &&
            sql.Message.Contains("FK_Tasks_Users", StringComparison.Ordinal))
        {
            // The required assignee may have disappeared after the existence check.
            return false;
        }
    }

    public Task<TaskItem?> FindAsync(int id, CancellationToken cancellationToken) =>
        db.Tasks.Include(x => x.AssignedUser).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> SaveStatusAsync(TaskItem task, CancellationToken cancellationToken)
    {
        // Force the compare-and-set even for idempotent same-state requests.
        db.Entry(task).Property(x => x.Status).IsModified = true;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<TaskDto[]> ListAsync(TaskFilters filters, CancellationToken cancellationToken)
    {
        var query = db.Tasks.AsNoTracking();
        if (filters.UserId is { } userId) query = query.Where(x => x.AssignedUserId == userId);
        if (filters.Status is { } status) query = query.Where(x => x.Status == status);
        if (filters.Priority is { } priority)
            query = query.Where(x => EF.Functions.Collate(SqlJson.Value(x.AdditionalData, "$.priority")!, "Latin1_General_100_BIN2") == priority);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.Title, x.Status, x.AssignedUserId, AssignedUserName = x.AssignedUser.Name, x.CreatedAt, x.AdditionalData })
            .ToArrayAsync(cancellationToken);
        return rows.Select(x => new TaskDto(x.Id, x.Title, x.Status, x.AssignedUserId, x.AssignedUserName,
            DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc), JsonSerializer.Deserialize<JsonElement>(x.AdditionalData))).ToArray();
    }
}
