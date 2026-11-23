using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        Assert.NotNull(paths["/api/v1/sessions"]!["post"]!["responses"]!["415"]?["content"]?["application/problem+json"]?["example"]);
        Assert.NotNull(paths["/api/v1/sessions"]!["post"]!["security"]);
        Assert.Null(paths["/api/v1/auth/login"]!["post"]!["security"]);
        Assert.Null(paths["/api/v1/auth/register"]!["post"]!["responses"]!["401"]);
        foreach (var operation in paths.SelectMany(path => path.Value!.AsObject()).Select(method => method.Value!))
        {
            Assert.NotNull(operation["operationId"]);
            Assert.NotNull(operation["summary"]);
            Assert.NotNull(operation["description"]);
            Assert.Contains(operation["responses"]!.AsObject(), x => x.Key.StartsWith('2'));
            if (operation["requestBody"]?["content"]?["application/json"] is { } input)
                Assert.NotNull(input["example"]);

            foreach (var response in operation["responses"]!.AsObject())
            {
                var body = response.Value!["content"];
                if (response.Key == "204")
                    Assert.Null(body);
                if (body?["application/json"] is { } data)
                    Assert.NotNull(data["example"]);
                if (body?["application/problem+json"] is { } error)
                    Assert.Equal(response.Key, error["example"]!["status"]!.ToString());
            }
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

    [Fact]
    public async Task DevelopmentSwaggerUiLoadsTheExistingContractAndLocalAssets()
    {
        using var api = new ContractApi("Development");
        using var client = api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var entry = await client.GetAsync("/api/docs");
        Assert.Equal(HttpStatusCode.MovedPermanently, entry.StatusCode);
        var page = new Uri(entry.RequestMessage!.RequestUri!, entry.Headers.Location!);
        Assert.Equal("/api/docs/index.html", page.AbsolutePath);

        var html = await client.GetStringAsync(page);
        Assert.Contains("Focus Assistant API | Swagger UI", html);
        using var css = await client.GetAsync("/api/docs/swagger-ui.css");
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
        Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);
        var initializer = await client.GetStringAsync("/api/docs/index.js");
        var document = new Uri(page, "../../openapi/v1.json");
        Assert.Contains("../../openapi/v1.json", initializer);

        var spec = JsonNode.Parse(await client.GetStringAsync(document))!;
        Assert.Equal("3.1.1", spec["openapi"]!.GetValue<string>());
        Assert.Equal(42, spec["paths"]!.AsObject().Sum(path => path.Value!.AsObject().Count));
        Assert.Equal("bearer", spec["components"]!["securitySchemes"]!["Bearer"]!["scheme"]!.GetValue<string>());
        using var protectedOperation = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedOperation.StatusCode);
    }

    [Theory]
    [InlineData("Contract", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task SwaggerUiRoutesAreAbsentOutsideDevelopment(string environment, HttpStatusCode documentStatus)
    {
        using var api = new ContractApi(environment);
        // Inspect route availability independently of the fallback authorization challenge.
        using var app = api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.PostConfigure<AuthorizationOptions>(options => options.FallbackPolicy = null)));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        foreach (var path in new[] { "/api/docs", "/api/docs/index.html", "/api/docs/swagger-ui.css" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        using var document = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(documentStatus, document.StatusCode);
    }

    private sealed class ContractApi(string environment = "Contract") : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            if (environment == "Production")
            {
                builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:KeyDirectory"] = Path.Combine(Path.GetTempPath(), "focus-contract-keys", Guid.NewGuid().ToString("N"))
                }));
            }

            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:PostgreSQL"] = "Host=127.0.0.1;Database=focus_contract;Username=contract" }));
        }
    }
}
