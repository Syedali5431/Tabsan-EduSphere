FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore Tabsan.EduSphere.sln
RUN dotnet publish src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Plan M3.3 — a sans-serif font for annotated PDFs (the renderer falls back to fonts bundled with PDFsharp without it).
# Plan M11 — Arabic-script fonts so Urdu/Arabic PDFs (and names in any PDF) render: Noto Naskh/Sans Arabic, Nastaliq Urdu.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-dejavu-core fonts-noto-core fonts-noto-extra \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tabsan.EduSphere.API.dll"]
