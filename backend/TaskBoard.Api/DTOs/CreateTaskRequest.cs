using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;

namespace TaskBoard.Api.DTOs;

public sealed class CreateTaskRequest : IValidatableObject
{
    private string? title;

    [Required, StringLength(200)]
    public string? Title { get => title; init => title = value?.Trim(); }

    [Range(1, int.MaxValue)]
    public int AssignedUserId { get; init; }

    public JsonElement AdditionalData { get; init; } = JsonSerializer.SerializeToElement(new { });

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AdditionalData.ValueKind != JsonValueKind.Object)
        {
            yield return Error("AdditionalData must be a JSON object.");
            yield break;
        }
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in AdditionalData.EnumerateObject())
        {
            if (!names.Add(field.Name)) yield return Error("AdditionalData must not contain duplicate property names.");
            var valid = field.Name switch
            {
                "priority" => field.Value.ValueKind == JsonValueKind.String && TaskFilters.IsPriority(field.Value.GetString()),
                "dueDate" => field.Value.ValueKind == JsonValueKind.String && DateOnly.TryParseExact(
                    field.Value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                "tags" => field.Value.ValueKind == JsonValueKind.Array && field.Value.EnumerateArray().All(x => x.ValueKind == JsonValueKind.String),
                "metadata" => field.Value.ValueKind == JsonValueKind.Object,
                _ => true
            };
            if (!valid) yield return Error($"AdditionalData.{field.Name} has an invalid value or shape.");
        }
    }

    private static ValidationResult Error(string message) => new(message, [nameof(AdditionalData)]);
}
