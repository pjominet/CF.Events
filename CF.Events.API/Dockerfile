# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["CF.Events.API/CF.Events.API.csproj", "CF.Events.API/"]
COPY ["CF.Events.Shared/CF.Events.Shared.csproj", "CF.Events.Shared/"]
RUN dotnet restore "CF.Events.API/CF.Events.API.csproj"

# Copy the rest of the code and build the application
COPY CF.Events.API/ CF.Events.API/
COPY CF.Events.Shared/ CF.Events.Shared/
WORKDIR "/src/CF.Events.API"
RUN dotnet build "CF.Events.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CF.Events.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set custom port
ENV ASPNETCORE_HTTP_PORTS=8081

# Create a directory for the database and set permissions if needed
# The app uses "Data Source=events.db" by default.
# We can mount a volume to /app/data and change the connection string.
RUN mkdir -p /app/data /app/keys
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/events.db"

EXPOSE 8081
ENTRYPOINT ["dotnet", "CF.Events.API.dll"]
