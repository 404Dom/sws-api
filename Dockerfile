FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY SteamWorkshopStats/SteamWorkshopStats.csproj SteamWorkshopStats/
RUN dotnet restore "SteamWorkshopStats/SteamWorkshopStats.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "SteamWorkshopStats/SteamWorkshopStats.csproj" -c "$configuration" -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "SteamWorkshopStats/SteamWorkshopStats.csproj" -c "$configuration" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamWorkshopStats.dll"]
