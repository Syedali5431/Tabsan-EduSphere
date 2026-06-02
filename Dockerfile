# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

# ✅ Restore ONLY the API project (not solution)
RUN dotnet restore src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj

# ✅ Publish ONLY the API project
RUN dotnet publish src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -c Release -o /app/out /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/out .

CMD ["dotnet", "Tabsan.EduSphere.API.dll"]