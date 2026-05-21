# 1. Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Development

# 2. SDK build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project definitions for optimized package caching
COPY ["Nexhire.slnx", "./"]
COPY ["src/Host/Nexhire.Api/Nexhire.Api.csproj", "src/Host/Nexhire.Api/"]
COPY ["src/Shared/Nexhire.Shared.Core/Nexhire.Shared.Core.csproj", "src/Shared/Nexhire.Shared.Core/"]
COPY ["src/Shared/Nexhire.Shared.Infrastructure/Nexhire.Shared.Infrastructure.csproj", "src/Shared/Nexhire.Shared.Infrastructure/"]
COPY ["src/Modules/Users/Nexhire.Modules.Users.Core/Nexhire.Modules.Users.Core.csproj", "src/Modules/Users/Nexhire.Modules.Users.Core/"]
COPY ["src/Modules/Users/Nexhire.Modules.Users.Infrastructure/Nexhire.Modules.Users.Infrastructure.csproj", "src/Modules/Users/Nexhire.Modules.Users.Infrastructure/"]
COPY ["tests/Nexhire.ArchitectureTests/Nexhire.ArchitectureTests.csproj", "tests/Nexhire.ArchitectureTests/"]
COPY ["tests/Modules/Users/Nexhire.Modules.Users.Tests.Unit/Nexhire.Modules.Users.Tests.Unit.csproj", "tests/Modules/Users/Nexhire.Modules.Users.Tests.Unit/"]

# Restore dependencies
RUN dotnet restore

# Copy rest of the files and build
COPY . .
WORKDIR "/src/src/Host/Nexhire.Api"
RUN dotnet build "Nexhire.Api.csproj" -c Release -o /app/build

# 3. Publish application
FROM build AS publish
RUN dotnet publish "Nexhire.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final runtime container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexhire.Api.dll"]
