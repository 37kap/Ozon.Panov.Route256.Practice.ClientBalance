FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Ozon.Route256.OrderService.csproj", "Ozon.Route256.OrderService/"]
RUN dotnet restore "/src/Ozon.Route256.OrderService/Ozon.Route256.OrderService.csproj"
WORKDIR "/src/Ozon.Route256.OrderService"
COPY . .
RUN dotnet build "Ozon.Route256.OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Ozon.Route256.OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ozon.Route256.OrderService.dll"]
