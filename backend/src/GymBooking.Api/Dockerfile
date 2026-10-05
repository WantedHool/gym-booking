FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/GymBooking.Api/GymBooking.Api.csproj src/GymBooking.Api/
COPY src/GymBooking.Core/GymBooking.Core.csproj src/GymBooking.Core/
COPY src/GymBooking.Data/GymBooking.Data.csproj src/GymBooking.Data/
RUN dotnet restore src/GymBooking.Api/GymBooking.Api.csproj

COPY src/ src/
RUN dotnet publish src/GymBooking.Api/GymBooking.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet GymBooking.Api.dll"]
