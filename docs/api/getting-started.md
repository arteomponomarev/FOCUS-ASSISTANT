# Getting started

Install the .NET SDK specified in [global.json](../../global.json) and PostgreSQL 17,
or use [Compose](../deployment/backend.md). Create a development database dedicated to
this application. Supply `ConnectionStrings__PostgreSQL` using your environment or configure
`ConnectionStrings:PostgreSQL` in .NET User Secrets for Development. Do not put the
connection string in version control. See [database setup](../../README.md#5-database-setup).

From the repository root in PowerShell:

```powershell
dotnet restore "FOCUS ASSISTANT.slnx" --locked-mode
dotnet build "FOCUS ASSISTANT.slnx" --no-restore
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project Focus.Api --no-build --no-launch-profile -- --migrate
dotnet run --project Focus.Api --no-build --no-launch-profile --urls http://127.0.0.1:8080
```

`--migrate` is a separate one-shot job. The serving process never changes the schema.
Check `http://127.0.0.1:8080/health/ready` and
`http://127.0.0.1:8080/openapi/v1.json`. Continue with the
[integration tutorial](integration-tutorial.md) to register and make an authorized request.
