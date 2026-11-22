FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY Focus.Api/*.csproj Focus.Api/packages.lock.json Focus.Api/
COPY Focus.Application/*.csproj Focus.Application/packages.lock.json Focus.Application/
COPY Focus.Domain/*.csproj Focus.Domain/packages.lock.json Focus.Domain/
COPY Focus.Engine/*.csproj Focus.Engine/packages.lock.json Focus.Engine/
COPY Focus.Infrastructure/*.csproj Focus.Infrastructure/packages.lock.json Focus.Infrastructure/
RUN dotnet restore Focus.Api/Focus.Api.csproj --locked-mode
COPY Focus.Api/ Focus.Api/
COPY Focus.Application/ Focus.Application/
COPY Focus.Domain/ Focus.Domain/
COPY Focus.Engine/ Focus.Engine/
COPY Focus.Infrastructure/ Focus.Infrastructure/
RUN dotnet publish Focus.Api/Focus.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
USER root
RUN install -d -m 0700 -o "$APP_UID" -g "$APP_UID" /var/focus/keys
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s \
  CMD ["dotnet", "Focus.Api.dll", "--probe-ready"]
ENTRYPOINT ["dotnet", "Focus.Api.dll"]
