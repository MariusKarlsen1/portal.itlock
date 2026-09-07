FROM alpine/git:latest AS gitinfo
WORKDIR /repo
COPY .git .git
RUN git log -1 --date=iso-strict --pretty=format:'%H%x1f%ad%x1f%s%x1f%b' > /commit.txt || echo "" > /commit.txt

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/PortalItlock.Web/PortalItlock.Web.csproj src/PortalItlock.Web/
RUN dotnet restore src/PortalItlock.Web/PortalItlock.Web.csproj
COPY src/PortalItlock.Web/ src/PortalItlock.Web/
RUN dotnet publish src/PortalItlock.Web/PortalItlock.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=gitinfo /commit.txt .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PortalItlock.Web.dll"]
