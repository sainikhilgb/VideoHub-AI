# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/VideoHub.Api/VideoHub.Api.csproj", "src/VideoHub.Api/"]
RUN dotnet restore "src/VideoHub.Api/VideoHub.Api.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "src/VideoHub.Api/VideoHub.Api.csproj" -c Release -o /app/build
RUN dotnet publish "src/VideoHub.Api/VideoHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 5000 (we will configure ASP.NET to listen on this port)
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

# Install curl for the docker-compose healthcheck
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Ensure the app user owns the application files
RUN chown -R app:app /app

# The standard .NET 8 runtime image creates a non-root 'app' user automatically.
USER app

ENTRYPOINT ["dotnet", "VideoHub.Api.dll"]
