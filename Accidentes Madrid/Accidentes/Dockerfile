# compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# solo .csproj
COPY *.csproj .
RUN dotnet restore

# resto dle código
COPY . .
RUN dotnet publish -c Release -o /app/publish

# imagen final solo con runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Accidentes.dll"]