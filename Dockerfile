# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY src/TcellxFreedom.Domain/TcellxFreedom.Domain.csproj           src/TcellxFreedom.Domain/
COPY src/TcellxFreedom.Application/TcellxFreedom.Application.csproj src/TcellxFreedom.Application/
COPY src/TcellxFreedom.Infrastructure/TcellxFreedom.Infrastructure.csproj src/TcellxFreedom.Infrastructure/
COPY src/TcellxFreedom.API/TcellxFreedom.API.csproj                 src/TcellxFreedom.API/

RUN dotnet restore src/TcellxFreedom.API/TcellxFreedom.API.csproj

# Copy all source and publish
COPY . .
RUN dotnet publish src/TcellxFreedom.API/TcellxFreedom.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Cloud Run injects PORT — app reads it in Program.cs
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "TcellxFreedom.API.dll"]
