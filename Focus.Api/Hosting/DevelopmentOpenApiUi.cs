using Swashbuckle.AspNetCore.SwaggerUI;

namespace Focus.Api.Hosting;

internal static class DevelopmentOpenApiUi
{
    public static void UseDevelopmentOpenApiUi(this WebApplication app)
    {
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "api/docs";
            options.DocumentTitle = "Focus Assistant API | Swagger UI";
            options.SwaggerEndpoint("../../openapi/v1.json", "Focus Assistant API v1");
            options.DocExpansion(DocExpansion.List);
            options.EnableFilter();
            options.EnableDeepLinking();
            options.DisplayRequestDuration();
            options.ConfigObject.PersistAuthorization = false;
            options.ConfigObject.ValidatorUrl = null;
            options.HeadContent = """
                <style>
                    .swagger-ui .topbar { background: #231d35; }
                    .swagger-ui .btn.execute { background: #5232cd; border-color: #5232cd; }
                    .swagger-ui .btn.execute:hover { background: #4325af; }
                    .swagger-ui :focus-visible { outline: 3px solid #7657e6; outline-offset: 3px; }
                </style>
                """;
        });
    }
}
