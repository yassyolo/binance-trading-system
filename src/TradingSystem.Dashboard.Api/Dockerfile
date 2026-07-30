FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/TradingSystem.Dashboard.Api/TradingSystem.Dashboard.Api.csproj
RUN dotnet publish src/TradingSystem.Dashboard.Api/TradingSystem.Dashboard.Api.csproj -c Release -o /app/publish --no-restore
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TradingSystem.Dashboard.Api.dll"]
