# ProCargo API image. Build from the backend folder:
#   docker build -f src/ProCargo.API/Dockerfile -t procargo-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ProCargo.Domain/ProCargo.Domain.csproj src/ProCargo.Domain/
COPY src/ProCargo.Application/ProCargo.Application.csproj src/ProCargo.Application/
COPY src/ProCargo.Infrastructure/ProCargo.Infrastructure.csproj src/ProCargo.Infrastructure/
COPY src/ProCargo.API/ProCargo.API.csproj src/ProCargo.API/
RUN dotnet restore src/ProCargo.API/ProCargo.API.csproj

COPY src/ src/
RUN dotnet publish src/ProCargo.API/ProCargo.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Uploads and encryption keys. Mount a volume here so they survive restarts.
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
VOLUME /app/App_Data

USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProCargo.API.dll"]
