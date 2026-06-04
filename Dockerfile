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
# Chiseled (distroless) runtime: ~half the size, non-root, minimal CVE surface.
# The base (non -extra) tag is fine because the app sets InvariantGlobalization=true.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Wex.CardApi.dll"]
