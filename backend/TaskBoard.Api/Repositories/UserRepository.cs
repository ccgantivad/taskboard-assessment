using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Repositories;

public sealed class UserRepository(TaskBoardDbContext db) : IUserRepository
{
    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<UserDto[]> ListAsync(CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new UserDto(x.Id, x.Name, x.Email)).ToArrayAsync(cancellationToken);
}
