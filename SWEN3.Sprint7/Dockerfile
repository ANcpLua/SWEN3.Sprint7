FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["SWEN3.Sprint7/SWEN3.Sprint7.csproj", "SWEN3.Sprint7/"]
RUN dotnet restore "SWEN3.Sprint7/SWEN3.Sprint7.csproj"
COPY . .
WORKDIR "/src/SWEN3.Sprint7"
RUN dotnet build "SWEN3.Sprint7.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SWEN3.Sprint7.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "SWEN3.Sprint7.dll"]
