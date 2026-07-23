FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PACKAGES_TOKEN
ARG BUILD_VERSION=unknown
WORKDIR /src

COPY nuget.config ./
COPY app/ZelosHR.Api.csproj app/

RUN if [ -n "$PACKAGES_TOKEN" ]; then \
      dotnet nuget update source github-deladetech1 \
        --username x-access-token \
        --password "$PACKAGES_TOKEN" \
        --store-password-in-clear-text \
        --configfile nuget.config; \
    fi \
    && dotnet restore app/ZelosHR.Api.csproj --configfile nuget.config

COPY app/ app/
RUN dotnet publish app/ZelosHR.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG BUILD_VERSION=unknown
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8000
ENV BUILD_VERSION=${BUILD_VERSION}
EXPOSE 8000
RUN useradd --uid 1000 --create-home appuser 2>/dev/null || useradd -r appuser
USER appuser
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ZelosHR.Api.dll"]
