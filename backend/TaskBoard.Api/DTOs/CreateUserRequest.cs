using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Api.DTOs;

public sealed class CreateUserRequest
{
    private string? name;
    private string? email;

    [Required, StringLength(120)]
    public string? Name { get => name; init => name = value?.Trim(); }

    [Required, StringLength(254), EmailAddress]
    public string? Email { get => email; init => email = value?.Trim(); }
}
