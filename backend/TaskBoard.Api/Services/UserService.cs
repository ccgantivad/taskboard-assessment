using System.ComponentModel.DataAnnotations;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;

namespace TaskBoard.Api.Services;

public sealed class UserService(IUserRepository repository)
{
    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Validate at the service boundary as well as the HTTP boundary.
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        var user = new User { Name = request.Name!, Email = request.Email! };
        await repository.AddAsync(user, cancellationToken);
        return new UserDto(user.Id, user.Name, user.Email);
    }

    public Task<UserDto[]> ListAsync(CancellationToken cancellationToken) => repository.ListAsync(cancellationToken);
}
