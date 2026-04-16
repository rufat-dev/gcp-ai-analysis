# Build context: repository root (gcp-ai-analysis)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY sql ./sql
COPY src/SoilAiInsightsWorker ./src/SoilAiInsightsWorker
WORKDIR /src/src/SoilAiInsightsWorker
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SoilAiInsightsWorker.dll"]
