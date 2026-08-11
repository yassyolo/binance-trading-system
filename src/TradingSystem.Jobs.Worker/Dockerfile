FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/TradingSystem.Jobs.Worker/TradingSystem.Jobs.Worker.csproj
RUN dotnet publish src/TradingSystem.Jobs.Worker/TradingSystem.Jobs.Worker.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "TradingSystem.Jobs.Worker.dll"]
