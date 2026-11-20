# Autenticación y autorización

## 1. Obtener el token

`POST /api/Auth/login` — **público**, con rate limiting (10 intentos/min por IP, configurable).

Request (`application/json`):

```json
{ "correo": "docente@cecar.edu.co", "contrasena": "MiClave123" }
```

| Campo | Tipo | Reglas |
|---|---|---|
| `correo` | string | obligatorio, formato email |
| `contrasena` | string | obligatorio, mínimo 6 |

Respuesta **200**:

```json
{
  "message": "Login exitoso",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9....",
  "tokenType": "Bearer",
  "expiresAt": "2026-11-11T16:30:00Z",
  "usuario": {
    "idUsuario": 3,
    "nombres": "Luis",
    "apellidos": "Rodríguez",
    "correo": "docente@cecar.edu.co",
    "fechaRegistro": "2025-10-20T08:00:00",
    "tipoRol": "Docente",
    "rolId": 3
  }
}
```

`usuario` se mantiene igual que antes (más `rolId`, aditivo) para no romper a los clientes.

Errores:

| Status | `message` | Causa |
|---|---|---|
| 400 | errores de validación | correo con formato inválido, contraseña < 6 |
| 401 | `Correo o contraseña incorrectos` | correo inexistente **o** contraseña errónea (mismo mensaje y tiempo similar: no permite enumerar cuentas). También si el hash guardado no es BCrypt (seed antiguo) |
| 403 | `El usuario no tiene acceso a la aplicación` | contraseña correcta pero rol sin acceso (p. ej. Estudiante) |
| 429 | `Demasiados intentos...` | límite superado; cabecera `Retry-After: <segundos>` |

## 2. Enviar el token

En cada petición protegida:

```
Authorization: Bearer <token>
```

- Sin token / token mal formado / firma inválida / **expirado** → **401** (`WWW-Authenticate: Bearer`).
- Token válido pero sin permiso → **403**.
- Al recibir 401 el cliente debe borrar la sesión y volver al login (no hay refresh token, ver abajo).

## 3. Contenido del token

JWT firmado **HS256**. Claims:

| Claim | Ejemplo | Significado |
|---|---|---|
| `sub` | `"3"` | `idUsuario` (string) |
| `correo` | `"docente@cecar.edu.co"` | correo |
| `rol` | `"Docente"` | rol canónico: `Administrador` \| `Auxiliar` \| `Docente` \| `Tutor` |
| `nombre` | `"Luis Rodríguez"` | nombre para mostrar |
| `jti` | `"9f1c..."` | id único del token |
| `iss` / `aud` | `AgendamientoApi` / `AgendamientoClientes` | emisor / audiencia |
| `iat` / `nbf` / `exp` | epoch s | emisión / validez / expiración (**8 h**) |

Los clientes **no necesitan** decodificar el token: usen `usuario` y `expiresAt` de la respuesta de login, o `GET /api/Auth/me`.

## 4. `GET /api/Auth/me`

Cualquier usuario autenticado. Devuelve el usuario del token leído de la BD (datos frescos).

- **200**: `UsuarioResponse` (mismo objeto que `usuario` del login).
- **401**: sin token, token inválido/expirado, el usuario ya no existe, o la cuenta está **desactivada** (`message`: `La cuenta está desactivada. Contacte con un administrador`).

### 4.2 Cuentas desactivadas (`Usuario.activo`) **[nuevo]**

- Login con contraseña correcta de una cuenta desactivada → **403** `La cuenta está desactivada. Contacte con un administrador`. Con contraseña errónea → 401 genérico (no se revela el estado a quien no conoce la contraseña).
- **Revocación efectiva**: tras validar firma y caducidad, `JwtBearerEvents.OnTokenValidated` (`ValidacionCuentaJwt`) comprueba que la cuenta exista y esté activa, con caché en memoria de **30 s** por usuario (una consulta por clave primaria cada 30 s como máximo). `PATCH /Usuario/{id}/estado` y `DELETE /Usuario/{id}` invalidan la entrada al momento, así que en una sola instancia el efecto es inmediato; con varias instancias, como mucho 30 s. Si falla → **401** en cualquier endpoint protegido con el `message` concreto. Elegido frente a "solo bloquear el login" porque un token dura 8 h: sin esto, desactivar a alguien no tendría efecto real durante la jornada.
- `/Auth/me` además lee siempre la BD (sin caché).

Útil para restaurar sesión al abrir la app: si responde 401, ir al login.

Verificado (test `R11_me_da_401_si_el_usuario_del_token_fue_borrado`): usuario borrado → 401 `El usuario del token ya no existe`. **Cambio de rol**: `/me` devuelve el rol **actual** de la BD, pero las políticas siguen usando el claim `rol` del token hasta que expire (máx. 8 h). La web debe usar `tipoRol` de `/me` para decidir el menú; si difiere del rol con el que inició sesión, conviene forzar un nuevo login. Revocación inmediata: ver propuestas en cambios-y-migracion.md.

## 4.1 `PUT /api/Auth/contrasena` (cambiar la contraseña propia)

Administrador, Auxiliar, Docente y Tutor (política `Personal`). Cuerpo `{ "contrasenaActual": "...", "contrasenaNueva": "..." }`; respuesta **204**. Reglas y errores en [endpoints.md](endpoints.md#cambiarcontrasena-put-authcontrasena).

- Elegido `PUT /Auth/contrasena` (y no `POST /Auth/cambiar-contrasena`): reemplaza un atributo del recurso "usuario actual", no expone ids y es idempotente en efecto (repetirlo con la misma nueva da 400 "distinta de la actual", sin cambiar nada).
- Contraseña actual incorrecta → **400** `La contraseña actual no es correcta`. Se descartó 401 porque los clientes tratan 401 como "sesión caducada" y cerrarían la sesión.
- Rate limiting propio (`RateLimiting:CambioContrasena`, por defecto 5 intentos cada 900 s) **por usuario** (`sub`): con un token robado no se puede adivinar la contraseña actual a fuerza bruta. Para particionar por usuario, `UseRateLimiter` va ahora después de `UseAuthentication` (el login sigue particionando por IP).
- Los JWT ya emitidos siguen válidos hasta expirar (máx. 8 h): cambiar la contraseña **no** cierra otras sesiones. La web puede pedir un nuevo login tras el cambio si lo desea.

## 5. Refresh token

**No implementado** (decisión consciente): requiere una tabla nueva (tokens de refresco con rotación y revocación) y por tanto un cambio de esquema. Con 8 h de vida el usuario vuelve a iniciar sesión una vez por jornada. Propuesta en [cambios-y-migracion.md](cambios-y-migracion.md#propuestas-no-implementadas).

## 6. Políticas

| Política | Roles |
|---|---|
| *(por defecto)* | cualquier usuario autenticado — aplica a todo endpoint sin atributo explícito |
| `GestionWeb` | Administrador, Auxiliar |
| `SoloAdministrador` | Administrador (reservada; hoy ningún endpoint la usa directamente, ver Usuario) |
| `Personal` | Administrador, Auxiliar, Docente, Tutor |
| **Público** (`AllowAnonymous`) | `POST Auth/login`, `POST Auth/olvide-contrasena`, `GET Auth/restablecer-contrasena/validar`, `POST Auth/restablecer-contrasena`, `GET Encuesta/verificar/{token}`, `POST Encuesta/responder` |

### Decisión sobre Auxiliar y gestión de usuarios

Se pidió "crear/editar/borrar usuarios: solo Administrador". La web, sin embargo, deja al Auxiliar registrar **estudiantes** (menú "Usuarios"). Bloquearlo rompería ese flujo. Regla implementada en `UsuarioService`:

- **Administrador**: crea/edita/borra cualquier usuario.
- **Auxiliar** (desde 2026-11-20): **crear y editar** usuarios cuyo rol (actual **y** nuevo) es Estudiante, Docente o Tutor; **eliminar y activar/desactivar** solo Estudiantes (sin cambios). Crear, editar o promover a Administrador/Auxiliar → 403 `Solo un Administrador puede crear o modificar Administradores o Auxiliares`. Crear o promover a Administrador/Auxiliar/Docente/Tutor → **403** `Solo un Administrador puede crear, modificar o eliminar usuarios que no sean Estudiantes`.

Así se elimina la escalada de privilegios (un Auxiliar no puede crearse un Administrador) sin romper la web. Si se prefiere "solo Administrador" estricto, basta con cambiar la política de esas 3 acciones a `SoloAdministrador`.

Además: nadie puede borrarse a sí mismo (400). La misma regla rige `PATCH /Usuario/{id}/estado`: el Auxiliar solo habilita/deshabilita Estudiantes (403 en otro caso); nadie se desactiva a sí mismo (409) y no se puede desactivar al último Administrador activo (409; se comprueba en una transacción `Serializable` para que dos administradores no se desactiven mutuamente a la vez).

## 7. Matriz de roles por endpoint

Leyenda: ✅ permitido · ❌ 403 · 🌐 público · 👤 solo sus propios datos (Docente/Tutor; Admin/Aux ven cualquiera) · 🔑 solo tutorías en las que participa (regla de propiedad, abajo) · **(M)** lo usa MAUI · **(W)** lo usa la web.

### 7.1 Regla de propiedad de tutorías (fase 2)

Decisión de producto "Docente dueño + Tutor asignado". Implementada en `AgendamientoGestion.Logica/Seguridad/ReglasAccesoTutoria.cs` (un único helper, cubierto por `MigracionWebTests`):

| Rol | Tutorías a las que accede | Ver | Finalizar / retroalimentación / encuestas | Agregar o quitar estudiantes |
|---|---|---|---|---|
| Administrador, Auxiliar | todas | ✅ | ✅ | ✅ |
| Docente | asignadas a él (`Tutoria.usuarioId`) **o** agendadas en un horario suyo (`horarioUsuarioId`) | ✅ | ✅ | ✅ |
| Tutor | solo asignadas a él | ✅ | ✅ | ❌ `El Tutor no puede agregar ni quitar estudiantes de una tutoría` |

Fuera de su alcance → **403** `No tiene acceso a esta tutoría` (no 404: los ids no son secretos). Tutoría inexistente → 404 antes de comprobar propiedad.

Crear tutoría (`POST /Tutoria`): Admin/Aux en cualquier horario disponible; Docente solo en **sus** horarios (403); Tutor **403** (paridad MAUI).

| Endpoint | Anónimo | Admin | Auxiliar | Docente | Tutor | Política |
|---|---|---|---|---|---|---|
| `POST /Auth/login` (W)(M) | 🌐 | 🌐 | 🌐 | 🌐 | 🌐 | público + rate limit |
| `GET /Auth/me` | 401 | ✅ | ✅ | ✅ | ✅ | autenticado |
| `PUT /Auth/contrasena` (W) **nuevo** | 401 | ✅ propia | ✅ propia | ✅ propia | ✅ propia | Personal + rate limit por usuario |
| `POST /Usuario` (W) | 401 | ✅ | ✅ Estudiante, Docente, Tutor | ❌ | ❌ | GestionWeb + regla |
| `GET /Usuario` (W) (con o sin `page`/`pageSize`/`q`/`rol`/`activo`) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `PATCH /Usuario/{id}/estado` (W) **[nuevo]** | 401 | ✅ (409 sí mismo / último Admin) | ✅ solo Estudiante | ❌ | ❌ | GestionWeb + regla |
| `GET /Usuario/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Usuario/by-rol/{tipoRol}` (W)(M) | 401 | ✅ | ✅ | ✅ | ✅ | Personal |
| `PUT /Usuario/{id}` (W) | 401 | ✅ | ✅ Estudiante, Docente, Tutor (rol actual y nuevo) | ❌ | ❌ | GestionWeb + regla |
| `DELETE /Usuario/{id}` (W) **[cambio fase 4: definitiva]** | 401 | ✅ (400 sí mismo; 409 último Admin activo) | ✅ solo Estudiante | ❌ | ❌ | GestionWeb + regla |
| `POST /Horario` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Horario` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Horario/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Horario/disponibles` (W)(M) | 401 | ✅ | ✅ | ✅ | ✅ | Personal |
| `GET /Horario/usuario/{usuarioId}` (W)(M) | 401 | ✅ | ✅ | 👤 | 👤 | Personal + propiedad |
| `PUT /Horario/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `DELETE /Horario/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `POST /Tutoria` (W)(M) | 401 | ✅ | ✅ | ✅ solo en sus horarios | ❌ | Personal + regla |
| `GET /Tutoria` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Tutoria/{id}` (W)(M) | 401 | ✅ | ✅ | 🔑 | 🔑 | Personal + propiedad |
| `PUT /Tutoria/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `DELETE /Tutoria/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `POST /Tutoria/{id}/estudiantes` (W)(M) | 401 | ✅ | ✅ | 🔑 | ❌ | Personal + propiedad |
| `GET /Tutoria/{id}/estudiantes` (W)(M) | 401 | ✅ | ✅ | 🔑 | 🔑 | Personal + propiedad |
| `DELETE /Tutoria/{tutoriaId}/estudiantes/{estudianteId}` (W) | 401 | ✅ | ✅ | 🔑 | ❌ | Personal + propiedad (antes GestionWeb) |
| `GET /Tutoria/calendario/docente` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Tutoria/calendario/estudiante` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Tutoria/calendario/idioma-nivel` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Tutoria/estado/{estado}` | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Tutoria/estado/{estado}/usuario/{usuarioId}` (M)(W) | 401 | ✅ | ✅ | 👤 | 👤 | Personal + propiedad |
| `GET /Tutoria/agendadas/usuario/{usuarioId}` (W) **nuevo** | 401 | ✅ | ✅ | 👤 | 👤 | Personal + propiedad |
| `PUT /Tutoria/{id}/finalizar` (M)(W) | 401 | ✅ | ✅ | 🔑 | 🔑 | Personal + propiedad |
| `POST /Retroalimentacion` (W)(M) | 401 | ✅ | ✅ | 🔑 a su nombre | 🔑 a su nombre | Personal + propiedad |
| `GET /Retroalimentacion` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Retroalimentacion/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Retroalimentacion/tutoria/{tutoriaId}` (W) | 401 | ✅ | ✅ | 🔑 | 🔑 | Personal + propiedad (antes GestionWeb) |
| `GET /Retroalimentacion/usuario/{usuarioId}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `PUT /Retroalimentacion/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `DELETE /Retroalimentacion/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `POST /Encuesta/enviar/{tutoriaId}` (M) | 401 | ✅ | ✅ | 🔑 | 🔑 | Personal + propiedad |
| `GET /Encuesta/verificar/{token}` (W) | 🌐 | 🌐 | 🌐 | 🌐 | 🌐 | público |
| `POST /Encuesta/responder` (W) | 🌐 | 🌐 | 🌐 | 🌐 | 🌐 | público |
| `GET /Encuesta/tutoria/{tutoriaId}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Encuesta/estadisticas` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Encuesta/pendientes` (W) **[nuevo]** | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Atencion/atendidos` (W) **[nuevo]** | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `POST /Atencion/atendidos` (W) **[nuevo]** | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `DELETE /Atencion/atendidos/{tipo}/{clave}` (W) **[nuevo]** | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Metricas/generales` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Metricas/tutorias` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Notificacion` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Notificacion/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Notificacion/usuario/{usuarioId}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `DELETE /Notificacion/{id}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `POST /Notificacion/email/cambio/{tutoriaId}` | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |
| `GET /Notificacion/reporte-excel/{tutoriaId}` (W) | 401 | ✅ | ✅ | ❌ | ❌ | GestionWeb |

Los endpoints que usa MAUI siguen permitidos para Docente y Tutor **dentro de su alcance** (regla 7.1). Impacto en MAUI: el Tutor ya no puede agendar ni agregar estudiantes (MAUI no se lo ofrece), el Docente solo agenda en sus horarios (MAUI solo le muestra los suyos) y nadie opera sobre tutorías en las que no participa. MAUI siempre envía su propio `usuarioId` en los listados "por usuario".
