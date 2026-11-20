# Build context: repository root (needs Directory.*.props, global.json and seed/).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/api/ src/api/
RUN dotnet restore src/api/TenancyManager.Api/TenancyManager.Api.csproj
RUN dotnet publish src/api/TenancyManager.Api/TenancyManager.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
COPY seed/ ./seed/
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProvider__SeedPath=/app/seed
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "TenancyManager.Api.dll"]
