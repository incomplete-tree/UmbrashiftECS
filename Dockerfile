# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy repository source code
COPY . .

# Publish Blazor WebAssembly client
RUN dotnet publish UmbrashiftECS.Web/UmbrashiftECS.Web.csproj -c Release -o /app/client

# Publish ASP.NET Core server
RUN dotnet publish UmbrashiftECS.Web/Server/Server.csproj -c Release -o /app/server

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir Data && chown app:app Data
USER app
# Copy server application
COPY --from=build /app/server .

# Copy client static assets to wwwroot
COPY --from=build /app/client/wwwroot ./wwwroot

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Server.dll"]
