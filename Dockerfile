# Use the SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["pms.csproj", "./"]
RUN dotnet restore "./pms.csproj"

# Copy the rest of the code and build
COPY . .
RUN dotnet build "pms.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "pms.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: Use the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pms.dll"]
