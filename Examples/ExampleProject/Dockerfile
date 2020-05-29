FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
LABEL project="foo"
LABEL "label.with.equals"="foo=bar"
LABEL "foo"="bar" "bar"="baz" "bar"="ignored"

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["ExampleProject/ExampleProject.csproj", "ExampleProject/"]
RUN dotnet restore "ExampleProject/ExampleProject.csproj"
COPY . .
WORKDIR "/src/ExampleProject"
RUN dotnet build "ExampleProject.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ExampleProject.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExampleProjectAsm.dll"]