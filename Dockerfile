FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything first (simple + reliable for Render)
COPY . .

# Restore + build using correct csproj path
RUN dotnet restore Klikk/Klikk.csproj
RUN dotnet publish Klikk/Klikk.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Klikk.dll"]