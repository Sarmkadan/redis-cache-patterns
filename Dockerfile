# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /build

# Copy project files
COPY RedisCachePatterns.csproj .

# Restore dependencies
RUN dotnet restore RedisCachePatterns.csproj

# Copy source code
COPY . .

# Build in release mode
RUN dotnet build -c Release --no-restore

# Publish
RUN dotnet publish -c Release -o /app --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=builder /app .

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Run application
ENTRYPOINT ["dotnet", "RedisCachePatterns.dll"]

