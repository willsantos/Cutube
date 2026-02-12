# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore
COPY cutube.sln ./
COPY src/Cutube.Api/Cutube.Api.csproj src/Cutube.Api/
COPY src/Cutube.Contracts/Cutube.Contracts.csproj src/Cutube.Contracts/
COPY src/Cutube.Domain/Cutube.Domain.csproj src/Cutube.Domain/
COPY cutube/cutube.csproj cutube/
RUN dotnet restore src/Cutube.Api/Cutube.Api.csproj

# Copy source and build
COPY src/Cutube.Api/ src/Cutube.Api/
COPY src/Cutube.Contracts/ src/Cutube.Contracts/
COPY src/Cutube.Domain/ src/Cutube.Domain/
COPY cutube/ cutube/
RUN dotnet publish src/Cutube.Api/Cutube.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install runtime dependencies
RUN apk add --no-cache \
    curl \
    ffmpeg \
    deno \
    python3 \
    py3-pip

# Install yt-dlp
RUN pip3 install --no-cache-dir --break-system-packages yt-dlp

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
