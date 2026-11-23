using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Focus.Api.IntegrationTests;

public sealed class ContractTests
{
    [Fact]
    public async Task GeneratedOpenApiMatchesTheVersionedContract()
    {
        using var api = new ContractApi();
        using var client = api.CreateClient();
        var raw = await client.GetStringAsync("/openapi/v1.json");
        var generated = JsonNode.Parse(raw)!;
        var paths = generated["paths"]!.AsObject();
        var duration = generated["components"]!["schemas"]!["CreateSessionRequest"]!["properties"]!["plannedDurationSeconds"]!;
        Assert.Equal("integer", duration["type"]!.GetValue<string>());
        Assert.Equal("string", generated["components"]!["schemas"]!["ProblemDetails"]!["properties"]!["traceId"]!["type"]!.GetValue<string>());
        Assert.True(paths.ContainsKey("/api/v1/sessions/{id}/activity/batch"));
        Assert.Equal(42, paths.Sum(path => path.Value!.AsObject().Count));
        Assert.NotNull(paths["/api/v1/sessions/{id}/vision"]!["get"]!["security"]);
        Assert.NotNull(paths["/api/v1/sessions"]!["post"]!["responses"]!["415"]?["content"]?["application/problem+json"]);
        Assert.Null(paths["/api/v1/sessions"]!["post"]!["responses"]!["415"]?["content"]?["application/problem+json"]?["example"]);
        Assert.NotNull(paths["/api/v1/sessions"]!["post"]!["security"]);
        Assert.Null(paths["/api/v1/auth/login"]!["post"]!["security"]);
        foreach (var operation in paths.SelectMany(path => path.Value!.AsObject()).Select(method => method.Value))
        {
            Assert.NotNull(operation!["operationId"]);
            Assert.NotNull(operation["summary"]);
            Assert.NotNull(operation["responses"]);
            Assert.Contains(operation["responses"]!.AsObject(), x => x.Key.StartsWith('2'));
        }

        var expectedExamples = new (string Path, string Method, int Status)[]
        {
            ("/api/v1/auth/register", "post", 201),
            ("/api/v1/auth/login", "post", 200),
            ("/api/v1/auth/login", "post", 401),
            ("/api/v1/sessions", "get", 401),
            ("/api/v1/sessions", "post", 400),
            ("/api/v1/sessions/{id}", "get", 404),
            ("/api/v1/sessions/{id}/complete", "post", 200),
            ("/api/v1/sessions/{id}/complete", "post", 409),
            ("/api/v1/sessions/{id}/activity/batch", "post", 200)
        };
        foreach (var (path, method, status) in expectedExamples)
        {
            var mediaType = status < 300 ? "application/json" : "application/problem+json";
            var example = paths[path]![method]!["responses"]![status.ToString()]?["content"]?[mediaType]?["example"];
            Assert.NotNull(example);
            if (status >= 400)
                Assert.Equal(status, example["status"]!.GetValue<int>());
        }

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "FOCUS ASSISTANT.slnx")))
            root = root.Parent;
        Assert.NotNull(root);
        var file = Path.Combine(root.FullName, "docs", "api", "openapi.json");

        if (Environment.GetEnvironmentVariable("FOCUS_EXPORT_OPENAPI") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await File.WriteAllTextAsync(file, raw + Environment.NewLine);
        }
        else
        {
            var expected = JsonNode.Parse(await File.ReadAllTextAsync(file));
            Assert.True(JsonNode.DeepEquals(expected, generated), "OpenAPI changed. Regenerate the contract and review the diff.");
        }
    }

    private sealed class ContractApi : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Contract");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:PostgreSQL"] = "Host=127.0.0.1;Database=focus_contract;Username=contract" }));
        }
    }
}
