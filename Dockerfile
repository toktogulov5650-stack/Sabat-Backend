FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Sabat.slnx ./
COPY Sabat.API/Sabat.API.csproj Sabat.API/
COPY Sabat.Application/Sabat.Application.csproj Sabat.Application/
COPY Sabat.Domain/Sabat.Domain.csproj Sabat.Domain/
COPY Sabat.Infrastructure/Sabat.Infrastructure.csproj Sabat.Infrastructure/
RUN dotnet restore Sabat.API/Sabat.API.csproj

COPY . .
RUN dotnet publish Sabat.API/Sabat.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads && chown -R app:app /app/wwwroot
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Sabat.API.dll"]
