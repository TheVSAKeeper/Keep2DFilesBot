FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["Keep2DFilesBot/Keep2DFilesBot.csproj", "Keep2DFilesBot/"]
RUN dotnet restore "Keep2DFilesBot/Keep2DFilesBot.csproj"

COPY . .
WORKDIR /src/Keep2DFilesBot
RUN dotnet publish "Keep2DFilesBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

RUN groupadd --system bot \
    && useradd --system --gid bot --home /app bot \
    && mkdir -p /app/data /app/logs

COPY --from=build /app/publish .
RUN chown -R bot:bot /app

VOLUME ["/app/data", "/app/logs"]
USER bot

ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 CMD ["dotnet", "--info"]

ENTRYPOINT ["dotnet", "Keep2DFilesBot.dll"]
