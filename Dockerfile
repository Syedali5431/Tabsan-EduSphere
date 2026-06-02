# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj
RUN dotnet publish src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -c Release -o /app/out /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0
ENV EDUSPHERE_TENANT_ISOLATION_ENABLED=false
ENV EDUSPHERE_TENANT_ISOLATION_STRATEGY=SharedConfig
EXPOSE 8080

COPY --from=build /app/out .

ENTRYPOINT ["sh", "-c", "dotnet Tabsan.EduSphere.API.dll --urls=http://0.0.0.0:${PORT:-8080}"]