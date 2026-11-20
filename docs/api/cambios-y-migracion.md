# Cambios vs. contrato anterior y guía de migración

## Resumen de cambios

### BREAKING

| # | Cambio | Afecta a | Qué hacer |
|---|---|---|---|
| B1 | **Todos los endpoints exigen `Authorization: Bearer <token>`** excepto `POST Auth/login`, `GET Encuesta/verificar/{token}`, `POST Encuesta/responder`. Sin token → 401 | Web, MAUI | Guardar el token del login y enviarlo en cada petición |
| B2 | Autorización por rol: Docente/Tutor reciben **403** en endpoints de gestión web; Auxiliar solo puede crear/editar/borrar **Estudiantes** | Web (Auxiliar), MAUI | Ver matriz. MAUI no usa ningún endpoint bloqueado |
| B3 | `GET Horario/usuario/{id}` y `GET Tutoria/estado/{e}/usuario/{id}`: Docente/Tutor solo con **su propio id** (si no → 403) | MAUI | MAUI ya envía su id; sin cambios |
| B4 | Login: correo inexistente pasa de **404** a **401**, con el mismo mensaje que contraseña errónea (`Correo o contraseña incorrectos`). Rol sin acceso pasa de 401 a **403** | Web, MAUI | Tratar 401 como "credenciales inválidas" y 403 como "sin acceso" |
| B5 | Cuerpos de error: ahora `application/problem+json` (RFC 9457). Se **mantiene** `message` y `errors`; desaparece `details` (antes exponía el mensaje interno de la excepción en Encuesta) | Web | Seguir leyendo `message`; no depender de `details` |
| B6 | CORS: solo orígenes de `Cors:AllowedOrigins` (por defecto `http://localhost:5173`). Antes `AllowAnyOrigin` | Web desplegada en otro dominio | Añadir el dominio real en configuración del backend |
| B7 | Rutas inexistentes sin token → 401 en vez de 404 | — | Ninguna |

### BREAKING — fase 2 (migración MAUI → web, 2026-11-11)

| # | Cambio | Afecta a | Qué hacer |
|---|---|---|---|
| B8 | **Regla de propiedad** en `GET Tutoria/{id}`, `GET/POST Tutoria/{id}/estudiantes`, `PUT finalizar`, `POST Encuesta/enviar`, `POST Retroalimentacion`: Docente solo tutorías asignadas o de sus horarios; Tutor solo asignadas → si no, 403 | MAUI (solo si se manipulan ids), web Docente/Tutor | Usar solo ids obtenidos de los listados propios |
| B9 | Tutor: **403** en `POST Tutoria`, `POST/DELETE Tutoria/{id}/estudiantes` | MAUI (no le ofrece esas acciones) | Ninguna |
| B10 | `POST Tutoria`: horario no disponible → **409** (antes se creaba igual, compartiendo horario); Docente en horario ajeno → 403; `usuarioId` no Docente/Tutor → 400; fecha/hora fuera del horario → 400 | **Web de gestión** (`mapTutoriaToAPI` enviaba un estudiante como `usuarioId`; reutilizar un horario `Ocupado` ya no es posible) | Enviar el Docente/Tutor asignado; elegir horarios `Disponible`/`Libre`; `fechaTutoria` = fecha del horario + `horaInicio` |
| B11 | `POST Tutoria/{id}/estudiantes` y `POST Tutoria` con `estudianteIds`: superar `cupos` → **409** `Cupo completo: X de Y ocupados…` | Web, MAUI (tope fijo 5; con cupos 5 no cambia) | Mostrar el mensaje; usar `horarioCupos` |
| B12 | `POST/PUT Horario`: `cupos` debe ser entero 1–1000 → si no, 400 | Web de gestión | Enviar un número (como string) |
| B13 | `POST Retroalimentacion`: Docente/Tutor con `usuarioId` ajeno → 403; segunda retroalimentación del mismo usuario en la misma tutoría → **409** (todos los roles) | MAUI (siempre envía su id; reintentos que antes duplicaban ahora dan 409) | Tratar 409 como "ya registrada" |

### Cambios de comportamiento y aditivos — fase 2

| # | Cambio |
|---|---|
| C14 | **Nuevo** `GET Tutoria/agendadas/usuario/{usuarioId}?estado=` (R1) |
| C15 | `GET Tutoria/estado/{estado}/usuario/{id}` acepta **grupos de sinónimos** sin distinguir mayúsculas (`pendiente` = Pendiente+Programada+Activo; `finalizada` = Finalizada+Completada). Antes comparaba exacto: MAUI ahora ve también las `Programada`/`Completada` del seed (R7). No se renombran datos |
| C16 | `PUT Tutoria/{id}/finalizar` acepta cuerpo **opcional** `{ calificacion, comentario }`: crea la retroalimentación (autor = token) y finaliza en una transacción; sin cuerpo = igual que antes (R8) |
| C17 | `TutoriaCreate.estudianteIds` opcional: crear + asignar atómico (R9). La respuesta 201 incluye los estudiantes asignados |
| C18 | `TutoriaResponse` + `horarioUsuarioId`, `horarioUsuarioNombre`, `horarioUsuarioApellidos`, `horarioTitulo`, `horarioCupos` (R6) |
| C19 | `DELETE Tutoria/{id}/estudiantes/{eid}` y `GET Retroalimentacion/tutoria/{id}` pasan de GestionWeb a Personal + propiedad (más permisivo para el Docente/Tutor participante; Admin/Aux igual) (R2, R10) |
| C20 | `POST Tutoria` sin hora (`00:00`) guarda la `horaInicio` del horario; reserva del horario atómica (UPDATE condicional: dos reservas simultáneas → una 201, otra 409) |
| C21 | Notificaciones por correo: un fallo de SMTP con un destinatario se registra (ILogger) y **no corta** el envío a los demás. La fila `Notificacion` se sigue guardando solo si el correo salió (semántica sin cambios) |
| C22 | CORS: `http://localhost:4173` (preview de Vite) añadido a `Cors:AllowedOrigins` (R11) |
| C23 | **Nuevo (aditivo)** `PUT /Auth/contrasena`: cada usuario con acceso cambia su propia contraseña (204). Rate limit propio por usuario. `UseRateLimiter` se movió tras `UseAuthentication` (sin efecto en el login) |

### Fase 3 (2026-11-11): título opcional, paginación de usuarios, cuentas habilitadas, asuntos atendidos

**Requiere esquema**: `AgendamientoGestion.Persistencia/ScriptBD/2026-11-11_usuario_activo_y_asuntos_atendidos.sql` (idempotente, no destructivo: añade `Usuario.activo BIT NOT NULL DEFAULT 1` y la tabla `AsuntoAtendido`). La API de esta rama **no arranca bien contra una BD sin él** (EF consulta `Usuario.activo` en el login).

| # | Cambio | Tipo | Afecta a / qué hacer |
|---|---|---|---|
| C24 | `HorarioCreate.titulo` **opcional**: omitido/`null`/vacío → título generado `{espacio} · {dd/MM/yyyy} {HH:mm}–{HH:mm}` (≤ 100). Con texto → se respeta. PUT sin título: regenera si era generado, conserva si era personalizado | aditivo | Web: puede quitar el campo. MAUI/E2E que envían título: sin cambios |
| C25 | `GET /Usuario` acepta `page`, `pageSize` (1–100, def. 20), `q` (sin mayúsculas ni tildes), `rol`, `activo`; responde la página + `X-Total-Count` (expuesta por CORS). **Sin parámetros: idéntico a antes** | aditivo | Web: paginar la gestión de usuarios |
| C26 | `UsuarioResponse.activo` (bool) | aditivo | — |
| C27 | **Nuevo** `PATCH /Usuario/{id}/estado { activo }` (Admin: todos; Aux: solo Estudiantes; 409 sí mismo / último Admin activo) | aditivo | Web: botón habilitar/deshabilitar |
| B14 | Cuenta desactivada: login → **403**; tokens ya emitidos → **401** en cualquier endpoint (`message` `La cuenta está desactivada…`) | **BREAKING** (solo afecta a cuentas que alguien desactive; hoy todas están activas) | Web/MAUI: mostrar el `message` del 403 en el login; el 401 ya lleva a la pantalla de login |
| B15 | `GET /Usuario/by-rol/{rol}` **excluye cuentas desactivadas** por defecto (`?incluirInactivos=true` para verlas) | **BREAKING** de comportamiento (sin efecto mientras no haya desactivados) | Selectores de asignación: nada. Listados que deban mostrar todo: añadir el parámetro o usar `GET /Usuario?rol=` |
| B16 | `POST /Tutoria`, `PUT /Tutoria/{id}` (al reasignar) y `POST /Tutoria/{id}/estudiantes`: asignado o estudiante desactivado → **400** | **BREAKING** de comportamiento | Mostrar el `message` |
| C28 | **Nuevo** `GET/POST /Atencion/atendidos`, `DELETE /Atencion/atendidos/{tipo}/{clave}`: atendidos del dashboard compartidos en servidor | aditivo | Web: sustituir el almacenamiento local de "atendido" |
| C29 | **Nuevo** `GET /Encuesta/pendientes`: tutorías con encuestas sin responder (clave del asunto `encuestas-sin-responder`) | aditivo | Web: usar sus `tutoriaId` como claves |

### Fase 4 (2026-11-12): eliminación definitiva de usuarios y correos

**Requiere esquema**: `AgendamientoGestion.Persistencia/ScriptBD/2026-11-12_eliminacion_usuarios_fk.sql` (idempotente; no modifica filas). Columnas `Usuario_idUsuario` de Tutoria, Horario, Retroalimentacion, Informe y AsuntoAtendido pasan a **NULL** con `ON DELETE SET NULL`; TutoriaEstudiante y Notificacion pasan a `ON DELETE CASCADE`. Aplicado en la BD de desarrollo el 2026-11-12 tras backup.

| # | Cambio | Tipo | Afecta a / qué hacer |
|---|---|---|---|
| B17 | `DELETE /Usuario/{id}` ya **no** devuelve 409 por registros asociados: elimina definitivamente (datos propios borrados, compartidos desvinculados). Respuesta 200 añade `resumen`. Nuevo 409 `No se puede eliminar al último Administrador activo` | **BREAKING** de comportamiento | Web: pedir confirmación clara antes de eliminar; mostrar el `resumen`. Ya no hace falta el mensaje "tiene registros asociados" |
| B18 | Campos de usuario **nullables** en respuestas: TutoriaResponse `usuarioId`/`usuarioNombre`/`usuarioApellidos`/`usuarioCorreo`/`horarioUsuarioId`/`horarioUsuarioNombre`/`horarioUsuarioApellidos`; HorarioResponse `usuarioId`/`usuarioNombre`/`usuarioApellidos`/`usuarioCorreo`; RetroalimentacionResponse `usuarioId`/`usuarioNombre`/`usuarioApellidos`; AsuntoAtendido `usuarioId`/`usuarioNombre` (antes `""`) | **BREAKING** de tipos (solo aparecen `null` tras eliminar a alguien) | Web y MAUI: tratar `null` como "Usuario eliminado"/"Sin asignar" (ver endpoints.md, "Datos desvinculados"). `horarioUsuarioId` sin horario pasa de `0` a `null` |
| C30 | Correos: MailKit, multipart/alternative (texto + HTML) UTF-8, From `CDITutorPro – CECAR <cuenta autenticada>`, Reply-To configurable (`SmtpSettings:ResponderA`), Message-ID y Date correctos, `Auto-Submitted`, reintentos y timeout. Asunto del recordatorio unificado a `Recordatorio de Tutoría - dd/MM/yyyy HH:mm` | interno | Ver `docs/correo/` |
| C31 | Plantillas de correo en `AgendamientoGestion.Logica/Correo/Plantillas` (layout + una por tipo, escape automático) y previsualización `GET /dev/correos` (solo Development) | aditivo | Alistar: rediseño en esas plantillas |

### Fase 5 (2026-11-20): "¿Olvidaste tu contraseña?" y Auxiliar gestiona personal académico

**Requiere esquema**: `ScriptBD/2026-11-20_restablecimiento_contrasena.sql` (idempotente, no destructivo, sin `USE`): columna `Usuario.credencialesCambiadasEn DATETIME2 NULL` y tabla `RestablecimientoContrasena`. `BD.sql` consolidado actualizado.

| # | Cambio | Tipo |
|---|---|---|
| C32 | Nuevos `POST /Auth/olvide-contrasena`, `GET /Auth/restablecer-contrasena/validar`, `POST /Auth/restablecer-contrasena` (públicos) y correos `restablecer-contrasena` / `contrasena-cambiada` | aditivo |
| C33 | Tras restablecer la contraseña, los JWT emitidos antes → 401 `La contraseña de la cuenta se cambió. Inicia sesión de nuevo` (el cambio con `PUT /Auth/contrasena` sigue sin cerrar sesiones) | comportamiento |
| C34 | Auxiliar: `POST`/`PUT /Usuario` también para Docentes y Tutores; 403 con Administrador/Auxiliar (mensaje nuevo `Solo un Administrador puede crear o modificar Administradores o Auxiliares`). Eliminar y activar/desactivar: solo Estudiantes (sin cambios) | permisos (más permisivo) |
| C35 | Configuración nueva: `Web:UrlBase` (Render: `Web__UrlBase=https://web-cid-proyecto-movil.pages.dev`) y sección `RestablecimientoContrasena` | config |

### Cambios de comportamiento (no rompen a los clientes actuales)

| # | Cambio |
|---|---|
| C1 | `PUT Usuario/{id}`: `contrasena` opcional (omitida/null/"" = conserva la actual). Antes era obligatoria y siempre se re-hasheaba. Correo de otro usuario → 409 |
| C2 | `POST Retroalimentacion`: `comentario` opcional. **Antes MAUI enviaba `""` y recibía 400**, la retroalimentación no se guardaba y la tutoría no se finalizaba |
| C3 | `POST Encuesta/responder`: `comentario` puede ser `null` (antes 400 implícito) |
| C4 | `POST Encuesta/enviar/{id}` idempotente: ya no duplica encuestas tras `finalizar`; respuesta añade `enviadas` y `yaExistentes` |
| C5 | `DELETE Tutoria/{id}` borra también las encuestas (antes 500 por FK) |
| C6 | `DELETE Usuario/{id}` / `DELETE Horario/{id}` con registros asociados → 409 con mensaje (antes 500) |
| C7 | `POST Tutoria/{id}/estudiantes` es todo-o-nada: si un id falla, no se asigna ninguno (antes quedaba a medias) |
| C8 | Métricas tolerantes a sinónimos de estado; `GET Metricas/tutorias` ya no escribe en `Informe` |
| C9 | `GET Horario/disponibles` incluye estado `Libre` además de `Disponible` |
| C10 | Recordatorios automáticos también para tutorías `Pendiente` (antes solo `programada`/`activo`, que los clientes nunca usan → nunca se enviaban) |
| C11 | Login con hash no BCrypt (seed antiguo) → 401 en vez de 500. Correo con formato inválido en servicios → 400 en vez de 500 |
| C12 | Enlace de encuesta configurable (`Encuesta:UrlBase`) |
| C13 | `TutoriaResponse.estudiantes` en POST/PUT es `[]` en vez de `null` |

### Aditivos

- `POST Auth/login`: `token`, `tokenType`, `expiresAt`.
- **Nuevo** `GET Auth/me`.
- `UsuarioResponse.rolId`, `TutoriaResponse.usuarioId` y `horarioId`, `HorarioResponse.usuarioId` (y `usuarioCorreo` en listados), `RetroalimentacionResponse.usuarioId` y `tutoriaId`.
- Todos los errores incluyen `traceId`.
- CORS expone `Content-Disposition` (nombre del Excel), `Retry-After` y `X-Total-Count`.

---

## Guía de migración — Web React (`webv1/src`)

La web **ya** guarda `response.token` en `localStorage('authToken')` (`services/apiService.js`) y `config/apiConfig.js` ya añade `Authorization: Bearer` si existe. Por tanto el login y las llamadas funcionan sin cambios. Pendiente:

1. **Manejo de 401 global** en `handleResponse`: si `status === 401` y la petición no es el login → `localStorage.removeItem('authToken'); localStorage.removeItem('user')` y redirigir a `/login` ("Sesión expirada").
2. **Expiración proactiva**: guardar `expiresAt` del login y, al cargar la app (`AuthContext`), si ya pasó → logout. Opcional: llamar `GET /api/Auth/me` al iniciar para validar la sesión restaurada desde `localStorage`.
3. **Mensajes 403**: hoy `handleResponse` interpreta 403 como problema de CORS. Ahora 403 significa "sin permisos": usar `errorData.message`.
4. **Login**: 401 → mostrar `message` ("Correo o contraseña incorrectos"); 403 → "sin acceso". Ya no hay 404 en login.
5. **Auxiliar**: al crear/editar/borrar usuarios que no son Estudiante recibirá 403; ocultar esas acciones para Auxiliar (`hasRole(['Administrador'])`).
6. **Editar usuario**: ya puede enviar `contrasena: ''` o omitirla para no cambiarla. Ojo: `DataContext` envía `'TempPass123!'` al editar estudiantes, lo que **resetea** su contraseña en cada edición; recomendable no enviarla.
7. **Errores**: seguir leyendo `message` y `errors`; `details` ya no existe.
8. **Página pública de encuesta**: no requiere token. Si el navegador tiene un token caducado o inválido en `localStorage` y se envía, los endpoints públicos lo ignoran (verificado en pruebas). Aun así, es más limpio no enviar `Authorization` en `encuestaService.verificar/responder`.
9. **Excel**: el nombre de archivo de `Content-Disposition` ahora es legible desde el navegador.
10. **Producción**: indicar al backend el dominio de la web para `Cors:AllowedOrigins` y para `Encuesta:UrlBase`.

## Guía de migración — MAUI (`Taller 3/Services`)

MAUI **no envía token** hoy: tras desplegar el backend, **todas** sus llamadas (salvo login) devolverán 401. Cambios necesarios:

1. `LoginResponse` (en `ApiService.cs`): añadir `public string token { get; set; }` y `public DateTime expiresAt { get; set; }`.
2. `ApiService.LoginAsync`: tras leer la respuesta, guardar el token (p. ej. `SecureStorage.SetAsync("authToken", token)`) y fijarlo en el `HttpClient`:
   `_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);`
   (el `HttpClient` es `Singleton` vía `IHttpClientFactory`; alternativa más limpia: un `DelegatingHandler` que lea el token de `SecureStorage` y lo añada en cada petición).
3. Logout (`AuthService`): borrar el token y la cabecera.
4. **401** en cualquier llamada → volver al login ("sesión expirada"). Hoy `EnsureSuccessStatusCode` lanza `HttpRequestException`; comprobar `response.StatusCode == HttpStatusCode.Unauthorized` antes.
5. Login: un correo inexistente ahora da **401** (antes 404) y un Estudiante **403**; ajustar el mensaje mostrado.
6. `CalificarTutoriaViewModel`: la llamada `_ = _apiService.EnviarEncuestasAsync(...)` tras `FinalizarTutoriaAsync` ya es inofensiva (idempotente) y **puede eliminarse**, porque `finalizar` ya envía las encuestas.
7. `RetroalimentacionCreateDto` con `Comentario = ""` ahora se acepta (antes 400: ese era el motivo de "No se pudo guardar la retroalimentación" sin comentario).
8. Modelos: pueden añadir `UsuarioId`/`HorarioId` en `TutoriaResponseDto`, `UsuarioId` en `HorarioResponseDto` y `RolId` en `UsuarioResponseDto` (opcional, aditivo).
9. Seguir enviando el propio `IdUsuario` en `Horario/usuario/{id}` y `Tutoria/estado/{e}/usuario/{id}` (otro id → 403).
10. ngrok: la cabecera `ngrok-skip-browser-warning` sigue funcionando.

---

## Decisiones pendientes (producto)

### Estados oficiales
Hoy conviven en datos y código:

| Concepto | Valores encontrados | Quién los escribe |
|---|---|---|
| Tutoría por realizar | `Pendiente`, `Programada`, `Activo` | clientes → `Pendiente`; seed → `Programada` |
| Tutoría realizada | `Finalizada`, `Completada` | `PUT finalizar` → `Finalizada`; seed → `Completada` |
| Cancelada | `Cancelada` | seed |
| Horario libre | `Disponible`, `Libre` | web/seed → `Disponible` |
| Horario ocupado | `Ocupado` | backend al crear tutoría |

No se renombró ningún valor guardado. Constantes centralizadas en `AgendamientoGestion.Entidades/Constants/EstadosTutoria.cs` y `EstadosHorario.cs`; métricas, recordatorios, `disponibles` y `finalizar` aceptan los sinónimos. **Cuando se decidan los oficiales**: script de migración de datos `UPDATE ... SET estado = ...`, validar `estado` como enum en los DTO y reducir los grupos de sinónimos a un valor. Desde la fase 2, `estado/{estado}/usuario/{id}` y `agendadas` filtran por grupo (C15); `GET estado/{estado}` (gestión web) sigue comparando el valor exacto.

### Auxiliar y gestión de usuarios
Implementado: Auxiliar solo gestiona Estudiantes (ver autenticación §6). Confirmar si se quiere "solo Administrador" estricto.

### Primer Administrador
Con autenticación obligatoria, `POST /Usuario` ya no es abierto. `BD.sql` **no crea ningún Administrador** y sus hashes no son BCrypt (esos usuarios no pueden iniciar sesión). Si la BD real no tiene ya un Administrador con hash BCrypt, hay que insertarlo a mano (hash generado con BCrypt, p. ej. desde una prueba o `BCrypt.Net.BCrypt.HashPassword`). No se automatizó para no escribir en la BD.

---

## Propuestas no implementadas

| Tema | Propuesta |
|---|---|
| Refresh token | Tabla `RefreshToken(id, usuarioId, hash, expira, revocado, reemplazadoPor)`, rotación en cada uso, revocación en logout/cambio de contraseña, token de acceso de 15–30 min. Requiere migración |
| Revocación inmediata | Hoy un token sigue siendo válido hasta 8 h aunque se borre el usuario o cambie su rol. Opción barata: comprobar en `OnTokenValidated` que el usuario existe y su rol coincide (1 consulta por petición, cacheable) |
| ~~Propiedad en tutorías~~ | **Implementado en fase 2** (regla "Docente dueño + Tutor asignado", ver autenticación §7.1) |
| `PUT Tutoria/{id}` (gestión web) | No aplica las validaciones nuevas de `POST` (horario disponible, rol del asignado, fecha dentro del horario, cupos) ni libera el horario anterior si se cambia. Propuesta: misma validación al cambiar `horarioId`/`usuarioId`/`fechaTutoria` |
| Reducir cupos | `PUT Horario` permite bajar `cupos` por debajo de los estudiantes ya asignados. Propuesta: 409 si `nuevoCupo < asignados` |
| Cupo con concurrencia | Implementado con bloqueo de fila de la tutoría (`UPDATE` sin cambio) dentro de la transacción: dos altas simultáneas sobre la misma tutoría se serializan |
| N+1 | Listados de tutorías hacen 1 consulta de estudiantes por tutoría; Retroalimentacion y Notificacion hacen consultas por elemento. Proyección con `Include`/`Select` en el repositorio |
| Paginación | `?page=&pageSize=` con envoltorio `{ items, page, pageSize, total }` en listados grandes (Usuario, Tutoria, Notificacion) — sería BREAKING, versionar |
| SMTP asíncrono | `SmtpClient.Send` es síncrono y bloquea la petición (crear tutoría, finalizar). Mover a cola (`Channel` + `BackgroundService`) o `SendMailAsync`; mejor proveedor transaccional |
| Migraciones EF | El esquema vive en `BD.sql`; adoptar migraciones EF Core para versionar cambios (índices únicos, tabla de refresh tokens) |
| Índices únicos | Script listo, **no ejecutado**: `AgendamientoGestion.Persistencia/ScriptBD/2026-11-11_indices_unicos_correo_encuesta.sql` |
| Estado de horario al borrar/editar tutoría | Al borrar una tutoría o cambiarle el horario, el horario anterior queda `Ocupado` |
| Versionado | Prefijo `/api/v1` antes del próximo cambio BREAKING |
