FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Ozon.Panov.Route256.Practice.ClientBalance/Ozon.Panov.Route256.Practice.ClientBalance.csproj", "src/Ozon.Panov.Route256.Practice.ClientBalance/"]
RUN dotnet restore "./src/Ozon.Panov.Route256.Practice.ClientBalance/Ozon.Panov.Route256.Practice.ClientBalance.csproj"
COPY . .
WORKDIR "/src/src/Ozon.Panov.Route256.Practice.ClientBalance"
RUN dotnet build "./Ozon.Panov.Route256.Practice.ClientBalance.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ozon.Panov.Route256.Practice.ClientBalance.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ozon.Panov.Route256.Practice.ClientBalance.dll"]