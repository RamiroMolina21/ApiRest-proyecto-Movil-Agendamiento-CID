# API Agendamiento — contrato v2 (JWT)

Rama: `feature/jwt-auth` · Fecha: 2026-11-11 · Fuente de verdad del backend (Dante) para la web React y la app MAUI.

| Documento | Contenido |
|---|---|
| [autenticacion-y-autorizacion.md](autenticacion-y-autorizacion.md) | Cómo obtener y enviar el token, claims, expiración, rate limiting, **matriz de roles por endpoint** |
| [endpoints.md](endpoints.md) | Contrato completo de los 49 endpoints: request, response, errores |
| [cambios-y-migracion.md](cambios-y-migracion.md) | Cambios vs. contrato anterior (**BREAKING** / aditivo), guía de migración web y MAUI, decisiones pendientes, propuestas |

## Convenciones generales

- **Base URL**: `{host}/api` (dev: `http://localhost:5208/api`; túnel ngrok: `https://<subdominio>.ngrok-free.dev/api`).
- **JSON**: propiedades en **camelCase** en las respuestas; en los cuerpos de petición el binder acepta cualquier mayúscula/minúscula (`Correo` o `correo`).
- **Fechas**: `DateTime` sin zona (hora local del servidor), formato `2026-11-11T08:00:00`. Excepción: `expiresAt` del login va en **UTC con `Z`**.
- **Números**: `calificacion` de Retroalimentacion es `decimal` (1–5, hasta 2 decimales); la de Encuesta es `int` (1–5).
- **Autenticación**: `Authorization: Bearer <token>` en **todos** los endpoints excepto los 3 públicos.
- **Sin paginación**: todos los listados devuelven el array completo (ver propuestas).

## Formato de error (todas las respuestas 4xx/5xx)

`Content-Type: application/problem+json` (RFC 9457) **más** `message` para compatibilidad con los clientes actuales:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Recurso no encontrado",
  "status": 404,
  "detail": "Tutoría no encontrada",
  "message": "Tutoría no encontrada",
  "traceId": "00-6b79...-00"
}
```

Errores de validación de modelo (400) añaden `errors` (mismo formato que antes, claves en PascalCase del DTO):

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Correo": ["El formato del correo no es válido"] },
  "message": "El formato del correo no es válido",
  "traceId": "..."
}
```

| Status | Cuándo |
|---|---|
| 400 | Validación de DTO, `ValidationException`, regla de negocio (p. ej. "Esta encuesta ya fue respondida") |
| 401 | Sin token, token inválido/expirado, o credenciales erróneas en login |
| 403 | Token válido pero rol sin permiso; o Docente/Tutor pidiendo datos de otro usuario; o login de un rol sin acceso (Estudiante) |
| 404 | Recurso inexistente |
| 409 | Correo duplicado; borrar usuario/horario con registros asociados |
| 429 | Demasiados intentos de login (cabecera `Retry-After` en segundos) |
| 500 | Error inesperado. `message` = "Error interno del servidor". `detail` solo trae el mensaje interno en `Development` |

> Las rutas inexistentes sin token devuelven **401** (la política por defecto exige autenticación antes de resolver 404).

## Configuración y arranque (backend)

Secretos fuera de `appsettings.json` (quedan como `""`). En desarrollo se usan **user-secrets** (proyecto `Agendamiento`, `UserSecretsId` ya configurado):

```powershell
cd "AgendamientoSolucion/Agendamiento"
# Clave JWT: mínimo 32 bytes aleatorios (generador criptográfico). En PowerShell:
#   $b = New-Object byte[] 48; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
dotnet user-secrets set "Jwt:Key" "<clave-aleatoria>"
dotnet user-secrets set "SmtpSettings:Remitente" "<correo remitente>"
dotnet user-secrets set "SmtpSettings:User" "<usuario smtp>"
dotnet user-secrets set "SmtpSettings:Password" "<contraseña de aplicación smtp>"
dotnet run --launch-profile http
```

Producción / servidor: variables de entorno `Jwt__Key`, `SmtpSettings__Password`, etc. (doble guion bajo = `:`).

Si falta `Jwt:Key` o mide menos de 32 bytes, la API **no arranca** y muestra:
`Falta la configuración 'Jwt:Key'. En desarrollo ejecute ... dotnet user-secrets set "Jwt:Key" ...`

Otras claves (en `appsettings.json`, sin secretos):

| Clave | Por defecto | Uso |
|---|---|---|
| `Jwt:Issuer` / `Jwt:Audience` | `AgendamientoApi` / `AgendamientoClientes` | Validación del token |
| `Jwt:ExpirationMinutes` | `480` (8 h) | Vida del token |
| `Cors:AllowedOrigins` | `["http://localhost:5173"]` | Orígenes del navegador permitidos (añadir el dominio real de la web en producción) |
| `RateLimiting:Login:PermitLimit` / `WindowSeconds` | `10` / `60` | Intentos de login por IP por ventana |
| `Encuesta:UrlBase` | `http://localhost:5173/encuesta` | Enlace del correo de encuesta (`?token=` se añade solo) |

Swagger (`/swagger`, solo en Development) tiene el botón **Authorize**: pegue el token sin el prefijo `Bearer`.

Pruebas: `dotnet test` en `AgendamientoSolucion` (xUnit + `WebApplicationFactory` + SQLite en memoria; nunca toca la BD real).

## Correo

- [Entregabilidad (spam, SPF/DKIM/DMARC, límites, producción)](../correo/entregabilidad.md)
- [Inventario de correos, plantillas y previsualización](../correo/plantillas-e-inventario.md)
