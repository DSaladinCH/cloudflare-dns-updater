FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CloudflareDNSUpdater/CloudflareDNSUpdater.csproj", "CloudflareDNSUpdater/"]
RUN dotnet restore "./CloudflareDNSUpdater/CloudflareDNSUpdater.csproj"
COPY . .
WORKDIR "/src/CloudflareDNSUpdater"
RUN dotnet build "./CloudflareDNSUpdater.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CloudflareDNSUpdater.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DSaladin.CloudflareDnsUpdater.dll"]