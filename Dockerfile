# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PortfolioApp.sln .
COPY PortfolioApp/PortfolioApp.csproj PortfolioApp/
RUN dotnet restore PortfolioApp/PortfolioApp.csproj

COPY PortfolioApp/ PortfolioApp/
RUN dotnet publish PortfolioApp/PortfolioApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway injects PORT at container start (varies per deploy), so it has to be read at runtime
# via shell expansion rather than baked into the image - falls back to 8080 outside Railway.
# App_Data (SQLite db + all uploaded models/watermark/PDFs/HDRI) resolves to /app/App_Data at
# runtime - mount a Railway Volume there or every redeploy wipes all admin-configured content.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet PortfolioApp.dll"]
