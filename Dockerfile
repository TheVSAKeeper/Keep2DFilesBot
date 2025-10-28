FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["Directory.Packages.props", "."]
COPY ["Keep2DFilesBot/Keep2DFilesBot.csproj", "Keep2DFilesBot/"]
RUN dotnet restore "Keep2DFilesBot/Keep2DFilesBot.csproj"

COPY . .
WORKDIR /src/Keep2DFilesBot
RUN dotnet publish "Keep2DFilesBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S bot \
    && adduser -S -G bot -h /app bot \
    && mkdir -p /app/data /app/logs

COPY --from=build /app/publish .
RUN chown -R bot:bot /app

VOLUME ["/app/data", "/app/logs"]

ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 CMD ["dotnet", "--info"]

ENTRYPOINT ["dotnet", "Keep2DFilesBot.dll"]
