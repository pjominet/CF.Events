# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project files and restore dependencies
COPY ["CF.Events.Web/CF.Events.Web.csproj", "CF.Events.Web/"]
COPY ["CF.Events.Shared/CF.Events.Shared.csproj", "CF.Events.Shared/"]
RUN dotnet restore "CF.Events.Web/CF.Events.Web.csproj"

# Copy the rest of the code and build the application
COPY CF.Events.Web/ CF.Events.Web/
COPY CF.Events.Shared/ CF.Events.Shared/
WORKDIR "/src/CF.Events.Web"
RUN dotnet build "CF.Events.Web.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CF.Events.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a directory for Data Protection keys
RUN mkdir -p /app/keys

# Set custom port
ENV ASPNETCORE_HTTP_PORTS=8082

EXPOSE 8082
ENTRYPOINT ["dotnet", "CF.Events.Web.dll"]
