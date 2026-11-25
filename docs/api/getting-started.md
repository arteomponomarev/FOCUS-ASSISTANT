# Getting started

Install the .NET SDK specified in [global.json](../../global.json) and PostgreSQL 17,
or use [Compose](../deployment/backend.md). Create a development database dedicated to
this application. Supply `ConnectionStrings__PostgreSQL` using your environment or configure
`ConnectionStrings:PostgreSQL` in .NET User Secrets for Development. Do not put the
connection string in version control. See [database setup](../../README.md#database-setup).

From the repository root in PowerShell:

```powershell
dotnet restore "FOCUS ASSISTANT.slnx" --locked-mode
dotnet build "FOCUS ASSISTANT.slnx" --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project Focus.Api --no-build --no-launch-profile -- --migrate
dotnet run --project Focus.Api --no-build --no-launch-profile -- --urls http://127.0.0.1:8080
```

`--migrate` is a separate one-shot job. The serving process never changes the schema.
Check `http://127.0.0.1:8080/health/ready` and
`http://127.0.0.1:8080/openapi/v1.json`. Continue with the
[integration tutorial](integration-tutorial.md) to register and make an authorized request.

## Try the API in Swagger UI

Open [Swagger UI on port 8080](http://127.0.0.1:8080/api/docs) after following the commands above. If using the checked-in launch profile, open [Swagger UI on port 55459](http://localhost:55459/api/docs).

1. Expand `POST /api/v1/auth/register`, choose **Try it out**, and register a disposable development account. Replace the example email and password with your own test values.
2. Expand `POST /api/v1/auth/login`, choose **Try it out**, and execute the request with that account.
3. Copy `accessToken` from the response. Select **Authorize**, paste the token by itself and confirm. The UI adds the `Bearer` prefix. The API issues ASP.NET Core opaque Bearer tokens.
4. Expand `GET /api/v1/auth/me` and choose **Try it out** and **Execute**. Expect HTTP 200 and your profile. Each operation shows its documented examples and schemas. **Execute** sends a real request, so write operations affect the connected development database.

The UI is available only in Development. Assets and requests stay on the API origin. Swagger UI does not persist authorization in browser storage. For production reference use the [versioned specification](openapi.json).
