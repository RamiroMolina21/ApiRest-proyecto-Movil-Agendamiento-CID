# API TutorPro (ASP.NET Core 8) - imagen para despliegue en contenedor (Render, etc.)
# Construir desde la raíz del repositorio:  docker build -t tutorpro-api .

# ---------- Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaurar primero (capa cacheable): solo los .csproj necesarios para la API
COPY AgendamientoSolucion/Agendamiento/Agendamiento.csproj AgendamientoSolucion/Agendamiento/
COPY AgendamientoSolucion/AgendamientoGestion.Logica/AgendamientoGestion.Logica.csproj AgendamientoSolucion/AgendamientoGestion.Logica/
COPY AgendamientoSolucion/AgendamientoGestion.Persistencia/AgendamientoGestion.Persistencia.csproj AgendamientoSolucion/AgendamientoGestion.Persistencia/
COPY AgendamientoSolucion/AgendamientoGestion.Entidades/AgendamientoGestion.Entidades.csproj AgendamientoSolucion/AgendamientoGestion.Entidades/
RUN dotnet restore AgendamientoSolucion/Agendamiento/Agendamiento.csproj

# Código fuente y publicación
COPY AgendamientoSolucion/ AgendamientoSolucion/
RUN dotnet publish AgendamientoSolucion/Agendamiento/Agendamiento.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Datos de globalización completos (fechas y textos en español)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Usuario sin privilegios incluido en la imagen oficial
USER $APP_UID

# Render inyecta la variable PORT; en local se usa 8080
EXPOSE 8080
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Agendamiento.dll"]
