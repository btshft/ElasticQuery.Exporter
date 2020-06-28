FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Exporter/ElasticQuery.Exporter.csproj", "Exporter/"]
RUN dotnet restore "Exporter/ElasticQuery.Exporter.csproj"
COPY . .
WORKDIR "/src/Exporter"
RUN dotnet build "ElasticQuery.Exporter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ElasticQuery.Exporter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ElasticQuery.Exporter.dll"]