using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Focus.Api.Hosting;

internal static class OpenApiDocumentation
{
    private static readonly JsonObject Notes = Read("ContractNotes.json");
    private static readonly JsonObject Examples = Read("ContractExamples.json");

    public static void Apply(OpenApiDocument document)
    {
        foreach (var (_, item) in document.Paths)
        {
            foreach (var (_, operation) in item.Operations ?? [])
                DescribeOperation(operation);
        }

        foreach (var (name, value) in document.Components?.Schemas?.ToArray() ?? [])
        {
            var schema = (OpenApiSchema)value;
            var note = Notes["schemas"]![name]!.AsObject();
            schema.Description = note["description"]!.GetValue<string>();
            foreach (var (property, field) in schema.Properties?.ToArray() ?? [])
            {
                var description = note["fields"]![property]!.GetValue<string>();
                if (field is OpenApiSchema inline)
                    inline.Description = description;
                else
                    schema.Properties![property] = new OpenApiSchema { Description = description, AllOf = [field] };
            }
        }
    }

    private static void DescribeOperation(OpenApiOperation operation)
    {
        var id = operation.OperationId!;
        var note = Notes["operations"]![id]!;
        var captured = Examples[id]!.AsObject();
        var auth = operation.Security is { Count: > 0 }
            ? " Requires a Bearer access token. Owned resources are scoped to the authenticated account."
            : " Does not require a Bearer access token.";
        operation.Description = note.GetValue<string>() + auth;

        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            var field = parameter.Name == "id" && id.StartsWith("Settings", StringComparison.Ordinal)
                ? "profileId"
                : parameter.Name!;
            parameter.Description = Notes["parameters"]![field]!.GetValue<string>();
        }

        if (operation.RequestBody?.Content?.TryGetValue("application/json", out var input) == true)
            input.Example = captured["request"]!.DeepClone();

        // Controller-wide metadata includes statuses that do not apply to these particular actions.
        var inapplicableStatus = id switch
        {
            "AuthRegister" => "401",
            "SessionsList" or "SettingsGetPomodoro" or "SettingsGetAvatar" or "SettingsGetProfiles" => "404",
            _ => null
        };
        if (inapplicableStatus is not null)
            operation.Responses!.Remove(inapplicableStatus);

        foreach (var (status, value) in operation.Responses ?? [])
        {
            if (value is not OpenApiResponse response || response.Content is not { } content)
                continue;
            var capturedBody = captured["responses"]?[status];
            if (status.StartsWith('2') && content.TryGetValue("application/json", out var data))
                data.Example = capturedBody!.DeepClone();
            if (content.TryGetValue("application/problem+json", out var problem))
            {
                var error = Notes["errors"]![status]!;
                response.Description = error["description"]!.GetValue<string>();
                problem.Example = capturedBody?.DeepClone() ?? error["example"]!.DeepClone();
            }
        }
    }

    private static JsonObject Read(string file)
    {
        using var stream = typeof(OpenApiDocumentation).Assembly.GetManifestResourceStream($"Focus.Api.Hosting.{file}")
            ?? throw new InvalidOperationException($"Missing OpenAPI documentation resource {file}.");
        return JsonNode.Parse(stream)!.AsObject();
    }
}
