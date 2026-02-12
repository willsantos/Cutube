# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore
COPY Cutube.sln ./
COPY src/Cutube.Api/Cutube.Api.csproj src/Cutube.Api/
COPY src/Cutube.Domain/Cutube.Domain.csproj src/Cutube.Domain/
RUN dotnet restore src/Cutube.Api/Cutube.Api.csproj

# Copy source and build
COPY src/Cutube.Api/ src/Cutube.Api/
COPY src/Cutube.Domain/ src/Cutube.Domain/
RUN dotnet publish src/Cutube.Api/Cutube.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apk add --no-cache curl

# Copy published app
COPY --from=build /app/publish .

# Create downloads directory
RUN mkdir -p /app/downloads

# Expose ports
EXPOSE 8080 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run
ENTRYPOINT ["dotnet", "Cutube.Api.dll"]
