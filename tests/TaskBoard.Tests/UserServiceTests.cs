using System.ComponentModel.DataAnnotations;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;
using TaskBoard.Api.Services;

namespace TaskBoard.Tests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task Create_normalizes_and_forwards_cancellation()
    {
        var repository = new RecordingRepository();
        var service = new UserService(repository);
        using var cancellation = new CancellationTokenSource();
        var created = await service.CreateAsync(new CreateUserRequest
        {
            Name = "  Synthetic User \t", Email = " synthetic@example.test "
        }, cancellation.Token);
        Assert.Equal(new UserDto(42, "Synthetic User", "synthetic@example.test"), created);
        Assert.Equal(cancellation.Token, repository.Token);
        Assert.Equal(1, repository.Writes);
    }

    [Theory]
    [InlineData(null, "valid@example.test")]
    [InlineData(" \t\r\n", "valid@example.test")]
    [InlineData("Valid", null)]
    [InlineData("Valid", "  ")]
    [InlineData("Valid", "invalid")]
    [InlineData("Valid", "a@@example.test")]
    public async Task Invalid_service_input_never_reaches_persistence(string? name, string? email)
    {
        var repository = new RecordingRepository();
        var service = new UserService(repository);
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateUserRequest { Name = name, Email = email }, CancellationToken.None));
        Assert.Equal(0, repository.Writes);
    }

    private sealed class RecordingRepository : IUserRepository
    {
        public int Writes { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Writes++;
            Token = cancellationToken;
            user.Id = 42;
            return Task.CompletedTask;
        }

        public Task<UserDto[]> ListAsync(CancellationToken cancellationToken) => Task.FromResult<UserDto[]>([]);
    }
}
