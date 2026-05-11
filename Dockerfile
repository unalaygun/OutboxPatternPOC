FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/OutboxPatternPOC.Api/OutboxPatternPOC.Api.csproj src/OutboxPatternPOC.Api/
RUN dotnet restore src/OutboxPatternPOC.Api/OutboxPatternPOC.Api.csproj

COPY src/ src/
RUN dotnet publish src/OutboxPatternPOC.Api/OutboxPatternPOC.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OutboxPatternPOC.Api.dll"]
