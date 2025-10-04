# Usa la imagen oficial de .NET 8 SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo del proyecto
COPY ["PortalAcademico01.csproj", "./"]
RUN dotnet restore "PortalAcademico01.csproj"

# Copia el resto del código
COPY . .
RUN dotnet build "PortalAcademico01.csproj" -c Release -o /app/build

# Publica la aplicación
FROM build AS publish
RUN dotnet publish "PortalAcademico01.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagen de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Crea el directorio para la base de datos
RUN mkdir -p /data

COPY --from=publish /app/publish .

# El puerto será proporcionado por Render a través de la variable PORT
EXPOSE 80

ENTRYPOINT ["dotnet", "PortalAcademico01.dll"]