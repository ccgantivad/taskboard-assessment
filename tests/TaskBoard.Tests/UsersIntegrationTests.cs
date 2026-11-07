using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBoard.Api.DTOs;
using TaskBoard.Api.Models;
using TaskBoard.Api.Repositories;

namespace TaskBoard.Tests;

public sealed class UsersIntegrationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Create_trims_persists_and_lists_without_a_location_or_unique_email_rule()
    {
        using var client = database.Factory.CreateClient();
        var email = $"users-{Guid.NewGuid():N}@example.test";
        var before = await database.UserCountAsync();
        using var response = await client.PostAsJsonAsync("/api/users", new { name = "  Synthetic User  ", email = $"  {email}  " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        var created = (await response.Content.ReadFromJsonAsync<UserDto>())!;
        Assert.True(created.Id > 0);
        Assert.Equal("Synthetic User", created.Name);
        Assert.Equal(email, created.Email);
        Assert.Equal((created.Name, created.Email), await database.ReadUserAsync(created.Id));

        using var duplicate = await client.PostAsJsonAsync("/api/users", new { name = "Another Synthetic User", email });
        Assert.Equal(HttpStatusCode.Created, duplicate.StatusCode);
        Assert.Equal(before + 2, await database.UserCountAsync());

        using var list = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var users = (await list.Content.ReadFromJsonAsync<UserDto[]>())!;
        Assert.Contains(users, user => user == created);
    }

    [Fact]
    public async Task Exact_length_limits_are_accepted_after_trimming()
    {
        using var client = database.Factory.CreateClient();
        var name = new string('n', 120);
        var email = new string('a', 64) + "@" + new string('b', 63) + "." + new string('c', 63) + "." + new string('d', 56) + ".test";
        Assert.Equal(254, email.Length);
        using var response = await client.PostAsJsonAsync("/api/users", new { name = $" {name} ", email = $" {email} " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<UserDto>())!;
        Assert.Equal((name, email), await database.ReadUserAsync(created.Id));
    }

    public static IEnumerable<object[]> InvalidRequests()
    {
        yield return ["{}"];
        yield return ["null"];
        yield return ["{\"name\":null,\"email\":\"valid@example.test\"}"];
        yield return ["{\"name\":\"  \\t\\n\",\"email\":\"valid@example.test\"}"];
        yield return [JsonSerializer.Serialize(new { name = new string('n', 121), email = "valid@example.test" })];
        yield return ["{\"name\":\"Synthetic\",\"email\":null}"];
        yield return ["{\"name\":\"Synthetic\",\"email\":\"  \"}"];
        yield return ["{\"name\":\"Synthetic\",\"email\":\"invalid\"}"];
        yield return ["{\"name\":\"Synthetic\",\"email\":\"a@@example.test\"}"];
        yield return [JsonSerializer.Serialize(new { name = "Synthetic", email = new string('a', 242) + "@example.test" })];
        yield return ["{\"name\":42,\"email\":\"valid@example.test\"}"];
        yield return ["{broken"];
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_requests_return_validation_problem_and_do_not_persist(string json)
    {
        using var client = database.Factory.CreateClient();
        var before = await database.UserCountAsync();
        using var response = await client.PostAsync("/api/users", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        foreach (var field in new[] { "type", "title", "detail", "traceId", "errors" })
            Assert.True(problem.RootElement.TryGetProperty(field, out _), field);
        Assert.Equal(before, await database.UserCountAsync());
    }

    [Fact]
    public async Task Unexpected_error_is_generic_problem_even_in_development()
    {
        await using var factory = database.Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.AddScoped<IUserRepository, FaultingRepository>();
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("internal-database-detail", body);
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(500, problem.RootElement.GetProperty("status").GetInt32());
        foreach (var field in new[] { "type", "title", "detail", "traceId" })
            Assert.True(problem.RootElement.TryGetProperty(field, out _), field);
    }

    private sealed class FaultingRepository : IUserRepository
    {
        public Task AddAsync(User user, CancellationToken cancellationToken) => throw new InvalidOperationException("internal-database-detail");
        public Task<UserDto[]> ListAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("internal-database-detail");
    }
}
