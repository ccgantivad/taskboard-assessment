using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBoard.Api.Data;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;

namespace TaskBoard.Tests;

public sealed class TasksIntegrationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private HttpClient Client() => database.Factory.CreateClient();

    private async Task<UserDto> CreateUserAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/users", new { name = "Synthetic Task Owner", email = $"task-{Guid.NewGuid():N}@example.test" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    private static async Task<TaskDto> CreateTaskAsync(HttpClient client, int userId, string priority = "High", string title = "Synthetic Task")
    {
        using var response = await client.PostAsJsonAsync("/api/tasks", new { title, assignedUserId = userId,
            additionalData = new { priority, dueDate = "2026-11-10", tags = new[] { "sql", "synthetic" }, metadata = new { source = "integration", arbitrary = new { value = 42 } } } });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<TaskDto>())!;
    }

    private async Task<int> CountAsync()
    {
        await using var scope = database.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>().Tasks.CountAsync();
    }

    private async Task<string> StatusAsync(int id)
    {
        await using var scope = database.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>().Tasks.Where(x => x.Id == id).Select(x => x.Status).SingleAsync();
    }

    private async Task SetStatusAsync(int id, string status)
    {
        await using var scope = database.Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>().Tasks.Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, status));
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
        foreach (var name in new[] { "type", "title", "detail", "traceId" })
            Assert.True(problem.RootElement.TryGetProperty(name, out _), name);
    }

    [Fact]
    public async Task Creation_returns_pending_user_name_utc_and_single_encoded_object()
    {
        using var client = Client();
        var user = await CreateUserAsync(client);
        var created = await CreateTaskAsync(client, user.Id, title: "  Synthetic Task  ");
        Assert.Equal("Synthetic Task", created.Title);
        Assert.Equal("Pending", created.Status);
        Assert.Equal(user.Name, created.AssignedUserName);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.InRange(created.CreatedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(JsonValueKind.Object, created.AdditionalData.ValueKind);
        Assert.Equal(42, created.AdditionalData.GetProperty("metadata").GetProperty("arbitrary").GetProperty("value").GetInt32());
        await using var scope = database.Factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>().Tasks.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.StartsWith("{", stored.AdditionalData);
        Assert.Equal("High", JsonSerializer.Deserialize<JsonElement>(stored.AdditionalData).GetProperty("priority").GetString());
        using var list = await client.GetAsync($"/api/tasks?userId={user.Id}");
        var raw = await list.Content.ReadAsStringAsync();
        Assert.Contains("Z\"", raw);
        var listed = (await list.Content.ReadFromJsonAsync<TaskDto[]>())!;
        Assert.Equal(created.Id, Assert.Single(listed).Id);
        Assert.Equal(created.CreatedAt, listed[0].CreatedAt);
    }

    [Fact]
    public async Task Omitted_metadata_defaults_to_empty_object_and_200_character_title_is_allowed()
    {
        using var client = Client();
        var user = await CreateUserAsync(client);
        using var response = await client.PostAsJsonAsync("/api/tasks", new { title = " " + new string('t', 200) + " ", assignedUserId = user.Id });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<TaskDto>())!;
        Assert.Equal(200, created.Title.Length);
        Assert.Equal("{}", created.AdditionalData.GetRawText());
    }

    public static IEnumerable<object[]> InvalidCreateFields()
    {
        foreach (var fragment in new[]
        {
            "\"title\":null", "\"title\":\" \\t\\n\"", "\"title\":42",
            "\"assignedUserId\":0", "\"assignedUserId\":-1", "\"assignedUserId\":2147483647", "\"assignedUserId\":null",
            "\"additionalData\":null", "\"additionalData\":[]", "\"additionalData\":\"{}\"", "\"additionalData\":42",
            "\"additionalData\":{\"priority\":\"high\"}", "\"additionalData\":{\"priority\":\"Urgent\"}",
            "\"additionalData\":{\"priority\":1}", "\"additionalData\":{\"priority\":null}",
            "\"additionalData\":{\"dueDate\":\"2026-02-30\"}", "\"additionalData\":{\"dueDate\":42}",
            "\"additionalData\":{\"tags\":\"sql\"}", "\"additionalData\":{\"tags\":[1]}",
            "\"additionalData\":{\"metadata\":[]}", "\"additionalData\":{\"metadata\":null}",
            "\"additionalData\":{\"priority\":\"High\",\"priority\":\"Low\"}"
        }) yield return [fragment];
        yield return ["\"title\":" + JsonSerializer.Serialize(new string('t', 201))];
    }

    [Theory]
    [MemberData(nameof(InvalidCreateFields))]
    public async Task Invalid_creation_does_not_persist(string fragment)
    {
        using var client = Client();
        var user = await CreateUserAsync(client);
        var json = "{" + (fragment.StartsWith("\"title\"") ? "" : "\"title\":\"Synthetic\",") +
            (fragment.StartsWith("\"assignedUserId\"") ? "" : $"\"assignedUserId\":{user.Id},") + fragment + "}";
        var before = await CountAsync();
        using var response = await client.PostAsync("/api/tasks", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(before, await CountAsync());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{broken")]
    [InlineData("{\"title\":\"Synthetic\"}")]
    public async Task Missing_or_malformed_creation_does_not_persist(string json)
    {
        using var client = Client();
        var before = await CountAsync();
        using var response = await client.PostAsync("/api/tasks", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(before, await CountAsync());
    }

    public static IEnumerable<object[]> TransitionMatrix() =>
        from fromStatus in new[] { "Pending", "InProgress", "Done" }
        from toStatus in new[] { "Pending", "InProgress", "Done" }
        select new object[] { fromStatus, toStatus };

    [Theory]
    [MemberData(nameof(TransitionMatrix))]
    public async Task Complete_transition_matrix_matches_persisted_state(string from, string to)
    {
        using var client = Client();
        var user = await CreateUserAsync(client);
        var task = await CreateTaskAsync(client, user.Id);
        await SetStatusAsync(task.Id, from);
        using var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = to });
        if (from == "Pending" && to == "Done")
        {
            await AssertProblemAsync(response, HttpStatusCode.Conflict);
            Assert.Equal("Pending", await StatusAsync(task.Id));
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(to, (await response.Content.ReadFromJsonAsync<TaskDto>())!.Status);
            Assert.Equal(to, await StatusAsync(task.Id));
        }
    }

    [Theory]
    [InlineData("{\"status\":\"pending\"}")]
    [InlineData("{\"status\":\"inprogress\"}")]
    [InlineData("{\"status\":\"DONE\"}")]
    [InlineData("{\"status\":\"Unknown\"}")]
    [InlineData("{\"status\":\"1\"}")]
    [InlineData("{\"status\":1}")]
    [InlineData("{\"status\":null}")]
    [InlineData("{\"status\":\" Pending \"}")]
    [InlineData("{}")]
    public async Task Invalid_status_does_not_change_persisted_state(string json)
    {
        using var client = Client();
        var user = await CreateUserAsync(client);
        var task = await CreateTaskAsync(client, user.Id);
        using var response = await client.PutAsync($"/api/tasks/{task.Id}/status", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Pending", await StatusAsync(task.Id));
    }

    [Fact]
    public async Task Unknown_task_returns_404()
    {
        using var client = Client();
        using var response = await client.PutAsJsonAsync("/api/tasks/2147483647/status", new { status = "InProgress" });
        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("status=pending")]
    [InlineData("status=1")]
    [InlineData("status=Unknown")]
    [InlineData("status=")]
    [InlineData("status=%20")]
    [InlineData("priority=high")]
    [InlineData("priority=Urgent")]
    [InlineData("priority=")]
    [InlineData("userId=0")]
    [InlineData("userId=-1")]
    [InlineData("userId=abc")]
    [InlineData("userId=")]
    [InlineData("userId=1&userId=2")]
    [InlineData("status=Pending&status=Done")]
    public async Task Invalid_filters_return_400(string query)
    {
        using var client = Client();
        using var response = await client.GetAsync("/api/tasks?" + query);
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Combined_filters_execute_json_value_in_sql_and_sort_by_date_then_id()
    {
        using var client = Client();
        var owner = await CreateUserAsync(client);
        var other = await CreateUserAsync(client);
        var older = await CreateTaskAsync(client, owner.Id);
        var newer = await CreateTaskAsync(client, owner.Id);
        var newest = await CreateTaskAsync(client, owner.Id);
        await CreateTaskAsync(client, owner.Id, "Low");
        var done = await CreateTaskAsync(client, owner.Id);
        await SetStatusAsync(done.Id, "Done");
        await CreateTaskAsync(client, other.Id);
        await using (var scope = database.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>();
            await db.Tasks.Where(x => x.Id == older.Id || x.Id == newer.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        }
        var capture = new SqlCapture();
        await using var factory = database.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<TaskBoardDbContext>(options => options.AddInterceptors(capture))));
        using var observed = factory.CreateClient();
        using var response = await observed.GetAsync($"/api/tasks?userId={owner.Id}&status=Pending&priority=High");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = (await response.Content.ReadFromJsonAsync<TaskDto[]>())!;
        Assert.Equal(new[] { newest.Id, newer.Id, older.Id }, tasks.Select(x => x.Id));
        var sql = Assert.Single(capture.Commands, x => x.Contains("JSON_VALUE", StringComparison.Ordinal));
        Assert.Contains("WHERE", sql);
        Assert.Contains("AssignedUserId] = @", sql);
        Assert.Contains("Status] = @", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("CreatedAt] DESC", sql);
        Assert.Contains("Id] DESC", sql);
        Assert.Contains("$.priority", sql);
        Assert.DoesNotContain("= N'High'", sql);
        using var emptyResponse = await client.GetAsync("/api/tasks?userId=2147483647");
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        Assert.Empty((await emptyResponse.Content.ReadFromJsonAsync<TaskDto[]>())!);
    }

    [Theory]
    [InlineData("Done")]
    [InlineData("InProgress")]
    public async Task Concurrent_status_changes_return_409_including_same_state_updates(string requested)
    {
        using var client = Client();
        var owner = await CreateUserAsync(client);
        var task = await CreateTaskAsync(client, owner.Id);
        await SetStatusAsync(task.Id, "InProgress");
        await using var racingFactory = database.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITaskRepository>();
            services.AddScoped<ITaskRepository>(sp => new RacingRepository(
                new TaskRepository(sp.GetRequiredService<TaskBoardDbContext>()), () => SetStatusAsync(task.Id, "Pending")));
        }));
        using var racingClient = racingFactory.CreateClient();
        using var response = await racingClient.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = requested });
        await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Pending", await StatusAsync(task.Id));
    }

    [Fact]
    public async Task Assignee_removed_after_validation_returns_400_without_task_persistence()
    {
        using var client = Client();
        var user = await CreateUserAsync(client);
        var before = await CountAsync();
        await using var factory = database.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITaskRepository>();
            services.AddScoped<ITaskRepository>(sp => new RacingRepository(
                new TaskRepository(sp.GetRequiredService<TaskBoardDbContext>()), afterUser: async () =>
                {
                    await using var scope = database.Factory.Services.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<TaskBoardDbContext>().Users
                        .Where(x => x.Id == user.Id).ExecuteDeleteAsync();
                }));
        }));
        using var racing = factory.CreateClient();
        using var response = await racing.PostAsJsonAsync("/api/tasks", new { title = "Synthetic FK race", assignedUserId = user.Id });
        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(before, await CountAsync());
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    public async Task Each_valid_priority_can_be_created_and_filtered(string priority)
    {
        using var client = Client();
        var owner = await CreateUserAsync(client);
        var task = await CreateTaskAsync(client, owner.Id, priority);
        using var response = await client.GetAsync($"/api/tasks?userId={owner.Id}&priority={priority}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(task.Id, Assert.Single((await response.Content.ReadFromJsonAsync<TaskDto[]>())!).Id);
    }

    private sealed class RacingRepository(ITaskRepository inner, Func<Task>? afterFind = null, Func<Task>? afterUser = null) : ITaskRepository
    {
        public async Task<bool> UserExistsAsync(int userId, CancellationToken ct)
        {
            var exists = await inner.UserExistsAsync(userId, ct);
            if (afterUser is not null) await afterUser();
            return exists;
        }
        public Task<bool> AddAsync(TaskItem task, CancellationToken ct) => inner.AddAsync(task, ct);
        public async Task<TaskItem?> FindAsync(int id, CancellationToken ct)
        {
            var task = await inner.FindAsync(id, ct);
            if (afterFind is not null) await afterFind();
            return task;
        }
        public Task<bool> SaveStatusAsync(TaskItem task, CancellationToken ct) => inner.SaveStatusAsync(task, ct);
        public Task<TaskDto[]> ListAsync(TaskFilters filters, CancellationToken ct) => inner.ListAsync(filters, ct);
    }

    private sealed class SqlCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
