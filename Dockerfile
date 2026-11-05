FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BioGamaEcuador.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish BioGamaEcuador.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/keys /app/.local/keys && chown -R app:app /app/keys /app/.local
USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s \
  CMD if [ -f /tls/certificate.pem ]; then curl -f --cacert /tls/certificate.pem https://localhost:8080/health; else curl -f http://localhost:8080/health; fi

ENTRYPOINT ["dotnet", "BioGamaEcuador.dll"]
