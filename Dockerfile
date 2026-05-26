FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first (better caching)
COPY Klikk/Klikk.csproj Klikk/

RUN dotnet restore Klikk/Klikk.csproj

# Copy everything
COPY . .

WORKDIR /src/Klikk
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Klikk.dll"]