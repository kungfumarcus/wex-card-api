# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (better layer caching)
COPY ["src/Wex.CardApi/Wex.CardApi.csproj", "src/Wex.CardApi/"]
RUN dotnet restore "src/Wex.CardApi/Wex.CardApi.csproj"

# Copy the rest and publish
COPY . .
RUN dotnet publish "src/Wex.CardApi/Wex.CardApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wex.CardApi.dll"]
