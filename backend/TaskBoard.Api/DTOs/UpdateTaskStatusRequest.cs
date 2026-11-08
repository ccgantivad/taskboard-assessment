using System.ComponentModel.DataAnnotations;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.DTOs;

public sealed class UpdateTaskStatusRequest : IValidatableObject
{
    [Required]
    public string? Status { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TaskStatuses.IsValid(Status))
            yield return new("Status must be Pending, InProgress or Done.", [nameof(Status)]);
    }
}
