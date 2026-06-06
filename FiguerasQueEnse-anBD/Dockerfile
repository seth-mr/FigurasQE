FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MicroservicioFiguras/MicroservicioFiguras.csproj MicroservicioFiguras/
RUN dotnet restore MicroservicioFiguras/MicroservicioFiguras.csproj

COPY MicroservicioFiguras/ MicroservicioFiguras/
RUN dotnet publish MicroservicioFiguras/MicroservicioFiguras.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5124

COPY --from=build /app/publish .

EXPOSE 5124
ENTRYPOINT ["dotnet", "MicroservicioFiguras.dll"]
