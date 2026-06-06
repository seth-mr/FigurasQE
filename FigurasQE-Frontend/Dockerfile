FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FigurasQE-WebClient.csproj ./
RUN dotnet restore ./FigurasQE-WebClient.csproj

COPY . ./
RUN dotnet publish ./FigurasQE-WebClient.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --retries=8 --start-period=20s \
    CMD curl --fail --silent --show-error http://127.0.0.1:8080/health || exit 1

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "FigurasQE-WebClient.dll"]
