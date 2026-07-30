FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/TradingSystem.Jobs.Worker/TradingSystem.Jobs.Worker.csproj
RUN dotnet publish src/TradingSystem.Jobs.Worker/TradingSystem.Jobs.Worker.csproj -c Release -o /app/publish --no-restore
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TradingSystem.Jobs.Worker.dll"]
