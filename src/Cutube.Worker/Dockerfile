# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore
COPY cutube.sln ./
COPY src/Cutube.Worker/Cutube.Worker.csproj src/Cutube.Worker/
COPY src/Cutube.Api/Cutube.Api.csproj src/Cutube.Api/
COPY src/Cutube.Domain/Cutube.Domain.csproj src/Cutube.Domain/
COPY cutube/cutube.csproj cutube/
RUN dotnet restore src/Cutube.Worker/Cutube.Worker.csproj

# Copy source and build
COPY src/Cutube.Worker/ src/Cutube.Worker/
COPY src/Cutube.Api/ src/Cutube.Api/
COPY src/Cutube.Domain/ src/Cutube.Domain/
COPY cutube/ cutube/
RUN dotnet publish src/Cutube.Worker/Cutube.Worker.csproj -c Release -o /app/publish -p:ErrorOnDuplicatePublishOutputFiles=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install dependencies
RUN apk add --no-cache \
    ffmpeg \
    python3 \
    py3-pip \
    curl

# Install yt-dlp
RUN pip3 install --no-cache-dir --break-system-packages yt-dlp

# Copy published app
COPY --from=build /app/publish .

# Create downloads directory
RUN mkdir -p /downloads

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD pgrep -x "dotnet" > /dev/null || exit 1

# Run
ENTRYPOINT ["dotnet", "Cutube.Worker.dll"]
