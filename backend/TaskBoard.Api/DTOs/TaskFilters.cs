using System.ComponentModel.DataAnnotations;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.DTOs;

public sealed class TaskFilters : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int? UserId { get; init; }
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Status { get; init; }
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Priority { get; init; }

    public static bool IsPriority(string? value) => value is "Low" or "Medium" or "High";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is not null && !TaskStatuses.IsValid(Status))
            yield return new("Status must be Pending, InProgress or Done.", [nameof(Status)]);
        if (Priority is not null && !IsPriority(Priority))
            yield return new("Priority must be Low, Medium or High.", [nameof(Priority)]);
    }
}
