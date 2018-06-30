FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY e6PollerBot/e6PollerBot.csproj e6PollerBot/
RUN dotnet restore e6PollerBot/e6PollerBot.csproj
COPY . .
WORKDIR /src/e6PollerBot
RUN dotnet build e6PollerBot.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish e6PollerBot.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "e6PollerBot.dll"]
