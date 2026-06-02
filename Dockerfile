# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Tabsan.EduSphere.sln
RUN dotnet publish src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -c Release -o /app/out /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080

COPY --from=build /app/out .

ENTRYPOINT ["sh", "-c", "dotnet Tabsan.EduSphere.API.dll --urls=http://0.0.0.0:${PORT:-8080}"]