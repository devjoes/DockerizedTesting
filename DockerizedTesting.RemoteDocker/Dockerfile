FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["DockerizedTesting.RemoteDocker/DockerizedTesting.RemoteDocker.csproj", "DockerizedTesting.RemoteDocker/"]
RUN dotnet restore "DockerizedTesting.RemoteDocker/DockerizedTesting.RemoteDocker.csproj"
COPY . .
WORKDIR "/src/DockerizedTesting.RemoteDocker"
RUN dotnet build "DockerizedTesting.RemoteDocker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DockerizedTesting.RemoteDocker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DockerizedTesting.RemoteDocker.dll"]