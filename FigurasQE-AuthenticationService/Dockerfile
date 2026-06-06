FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY FigurasQE-AuthenticationService.csproj .
RUN dotnet restore FigurasQE-AuthenticationService.csproj

COPY . .
RUN dotnet publish FigurasQE-AuthenticationService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:5041

COPY --from=build /app/publish .

EXPOSE 5041
ENTRYPOINT ["dotnet", "FigurasQE-AuthenticationService.dll"]
