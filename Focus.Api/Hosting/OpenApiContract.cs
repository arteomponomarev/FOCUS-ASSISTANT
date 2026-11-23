using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Focus.Api.Hosting;

internal static class OpenApiContract
{
    private const string Bearer = "Bearer";

    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "Focus Assistant API";
            document.Info.Version = "v1";
            document.Info.Description = "Session management and research data. Observed activity, heuristic estimates, and self-reports are separate.";
            document.AddComponent(Bearer, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "Opaque",
                Description = "Use the access token from login or refresh in the Authorization header."
            });
            OpenApiDocumentation.Apply(document);
            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
                return Task.CompletedTask;

            operation.OperationId = action.ControllerName + action.ActionName;
            operation.Summary = Summaries[operation.OperationId];
            if (!action.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(Bearer, context.Document)] = []
                    }
                ];
            }

            foreach (var (code, response) in operation.Responses ?? [])
            {
                if (!code.StartsWith('4') && !code.StartsWith('5'))
                    continue;
                if (response is OpenApiResponse error && error.Content?.TryGetValue("application/json", out var json) == true)
                {
                    error.Content.Clear();
                    error.Content.Add("application/problem+json", json);
                }
            }

            if (operation.RequestBody?.Content?.ContainsKey("application/json") == true &&
                operation.Responses is { } responses &&
                responses.TryGetValue("400", out var badRequest) &&
                badRequest is OpenApiResponse { Content: { } content } &&
                content.TryGetValue("application/problem+json", out var problem))
            {
                responses.TryAdd("415", new OpenApiResponse
                {
                    Description = "Unsupported request content type.",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new() { Schema = problem.Schema }
                    }
                });
            }

            return Task.CompletedTask;
        });

        options.AddSchemaTransformer((schema, context, _) =>
        {
            if (context.JsonTypeInfo.Type == typeof(ProblemDetails) && schema.Properties is { } properties)
            {
                properties.TryAdd("traceId", new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description = "Request correlation identifier, when available."
                });
            }

            if (context.JsonPropertyInfo?.AttributeProvider is not PropertyInfo property)
                return Task.CompletedTask;

            var attributes = property.GetCustomAttributes<ValidationAttribute>().Concat(
                property.DeclaringType!.GetConstructors()
                    .SelectMany(x => x.GetParameters())
                    .Where(x => string.Equals(x.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(x => x.GetCustomAttributes<ValidationAttribute>()));
            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case RangeAttribute range:
                        schema.Minimum = Convert.ToString(range.Minimum, CultureInfo.InvariantCulture);
                        schema.Maximum = Convert.ToString(range.Maximum, CultureInfo.InvariantCulture);
                        break;
                    case StringLengthAttribute length:
                        schema.MaxLength = length.MaximumLength;
                        if (length.MinimumLength > 0)
                            schema.MinLength = length.MinimumLength;
                        break;
                    case EmailAddressAttribute:
                        schema.Format = "email";
                        break;
                    case MaxLengthAttribute maximum:
                        schema.MaxItems = maximum.Length;
                        break;
                    case MinLengthAttribute minimum:
                        schema.MinItems = minimum.Length;
                        break;
                }
            }

            return Task.CompletedTask;
        });
    }

    private static readonly IReadOnlyDictionary<string, string> Summaries = new Dictionary<string, string>
    {
        ["AuthRegister"] = "Create an account and return its public profile.",
        ["AuthLogin"] = "Exchange account credentials for access and refresh tokens.",
        ["AuthRefresh"] = "Rotate a refresh token and revoke the previous token pair.",
        ["AuthLogout"] = "Revoke the current login and optionally withdraw camera consent for the account.",
        ["AuthMe"] = "Read the authenticated account profile.",
        ["AuthRename"] = "Update the authenticated account's display name.",
        ["AuthChangePassword"] = "Change the password and revoke all account logins.",
        ["SessionsCreate"] = "Create a planned session with optional goals and an owned work profile.",
        ["SessionsList"] = "Page through owned sessions by start or creation time.",
        ["SessionsGet"] = "Read one owned session with durations, ratings, and goal statistics.",
        ["SessionsStart"] = "Start a planned session.",
        ["SessionsPause"] = "Record the beginning of a pause in an active session.",
        ["SessionsResume"] = "End the active pause and resume work.",
        ["SessionsCancel"] = "Cancel a planned, active, or paused session.",
        ["SessionsComplete"] = "Finish a started session and atomically record ratings and goal outcomes.",
        ["SessionsListGoals"] = "List goals attached to an owned session.",
        ["SessionsGetGoal"] = "Read a goal by its ID within an owned session.",
        ["SessionsAddGoal"] = "Add a goal to a planned session.",
        ["SessionsUpdateGoal"] = "Edit a planned goal description or record a started goal's outcome.",
        ["SessionsDeleteGoal"] = "Remove a goal from a planned session.",
        ["ResearchUpload"] = "Store independently consented desktop or camera aggregates with retry deduplication.",
        ["ResearchVision"] = "Summarize coarse camera observation totals and compare baseline and experimental estimates for an owned session.",
        ["ResearchActivity"] = "Page through observed aggregate windows in chronological order.",
        ["ResearchEstimates"] = "Page through heuristic estimates in chronological order.",
        ["ResearchCurrent"] = "Read the latest available estimate, session status, and goals for an owned session.",
        ["ResearchTimeline"] = "Read lifecycle timestamps, goals, and a page of chronological interval references.",
        ["AnalyticsSummary"] = "Summarize observed activity, estimates, goals, and self-reports in a date range.",
        ["AnalyticsStatistics"] = "Return calendar day, ISO week, or month buckets in the requested IANA time zone.",
        ["AnalyticsResearch"] = "Page through comparable per-session research measures.",
        ["PrivacyGet"] = "Read independent desktop and camera consent and the experimental estimate preference.",
        ["PrivacyUpdate"] = "Change server-side acceptance or the vision experiment without opening a device.",
        ["PrivacyExport"] = "Read the account privacy export and links to pageable records.",
        ["PrivacyDeleteAccount"] = "Verify the account password and delete the account and its owned data.",
        ["SettingsGetPomodoro"] = "Read persisted Pomodoro preferences or the defaults.",
        ["SettingsSetPomodoro"] = "Replace the authenticated account's Pomodoro preferences.",
        ["SettingsGetAvatar"] = "Read persisted avatar preferences or the defaults.",
        ["SettingsSetAvatar"] = "Replace the authenticated account's avatar preferences.",
        ["SettingsGetProfiles"] = "Page through the account's work profiles and process rules.",
        ["SettingsGetProfile"] = "Read one owned work profile and its process rules.",
        ["SettingsCreateProfile"] = "Create a work profile and optional process rules.",
        ["SettingsReplaceProfile"] = "Replace an owned work profile and its process rules.",
        ["SettingsDeleteProfile"] = "Delete an unused profile. Sessions retain their profile references."
    };
}
