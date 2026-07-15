# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files + restore (layer caching)
COPY src/GymBooking.Api/GymBooking.Api.csproj src/GymBooking.Api/
COPY src/GymBooking.Core/GymBooking.Core.csproj src/GymBooking.Core/
COPY src/GymBooking.Data/GymBooking.Data.csproj src/GymBooking.Data/
RUN dotnet restore src/GymBooking.Api/GymBooking.Api.csproj

# Copy the rest + publish
COPY src/ src/
RUN dotnet publish src/GymBooking.Api/GymBooking.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Το Render περνάει το port μέσω env var PORT (τιμή γνωστή μόνο στο runtime — όχι στο build,
# άρα δεν μπορεί να μπει σε ENV με ${PORT:-...} substitution). Shell-form entrypoint ώστε το
# $PORT να διαβαστεί όταν ξεκινάει πραγματικά ο container.
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet GymBooking.Api.dll"]
