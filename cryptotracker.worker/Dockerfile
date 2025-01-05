# Stage 1: Base for building the .NET API
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS base-worker

COPY . /source
WORKDIR /source

# Set default architecture argument for multi-arch builds
ARG TARGETARCH

# Restore dependencies with cache
RUN dotnet restore

# Stage 2: Publish .NET API
FROM base-worker AS publish-worker

ARG TARGETARCH

# Publish the app for the target architecture
RUN dotnet publish --runtime linux-${TARGETARCH} --self-contained false -o /app

# Stage 4: Final production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS final

WORKDIR /app

# Copy .NET API and frontend assets
COPY --from=publish-worker /app ./

ENTRYPOINT ["dotnet", "cryptotracker.worker.dll"]