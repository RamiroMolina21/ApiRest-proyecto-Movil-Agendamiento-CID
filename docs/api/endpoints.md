# Contrato de endpoints

Todas las rutas cuelgan de `/api`. Autorización por endpoint: ver la [matriz](autenticacion-y-autorizacion.md#7-matriz-de-roles-por-endpoint). Errores: [formato común](README.md#formato-de-error-todas-las-respuestas-4xx5xx). Además de los errores listados, cualquier endpoint protegido puede devolver **401** (sin token/expirado) y **403** (rol).

Marcas: **[nuevo]** endpoint nuevo · **[+]** campo aditivo · **[cambio]** comportamiento distinto (ver [cambios](cambios-y-migracion.md)).

---

## Esquemas

### UsuarioResponse
```json
{
  "idUsuario": 3, "nombres": "Luis", "apellidos": "Rodríguez", "correo": "luis@cecar.edu.co",
  "fechaRegistro": "2025-10-20T08:00:00", "tipoRol": "Docente",
  "rolId": 3, "activo": true
}
```
`rolId` **[+]**. `activo` **[+]** (cuenta habilitada; `false` = no puede iniciar sesión ni ser asignada a tutorías). `tipoRol` puede ser `"Sin rol"` si el rol no existe.

### UsuarioCreate (POST /Usuario)
| Campo | Tipo | Reglas |
|---|---|---|
| `nombres` | string | obligatorio, ≤ 60 |
| `apellidos` | string | obligatorio, ≤ 60 |
| `correo` | string | obligatorio, email, ≤ 60, **único** |
| `contrasena` | string | obligatorio, ≥ 6 |
| `rolId` | int | obligatorio, debe existir (1 Administrador, 2 Tutor, 3 Docente, 4 Estudiante, 5 Auxiliar según BD.sql) |

### UsuarioUpdate (PUT /Usuario/{id}) **[cambio]**
Igual que UsuarioCreate salvo `contrasena`: **opcional** (omitida, `null` o `""` = se conserva la actual; si viene con contenido, ≥ 6 y ≤ 100).
**No toca `activo`** (un campo `activo` en el cuerpo se ignora): habilitar/deshabilitar solo con `PATCH /Usuario/{id}/estado`.

### UsuarioEstado (PATCH /Usuario/{id}/estado) **[nuevo]**
```json
{ "activo": false }
```
`activo` boolean **obligatorio** (falta o `null` → 400 `El campo activo es obligatorio (true o false)`).

### AsuntoAtendido (respuesta de /Atencion/atendidos) **[nuevo]**
```json
{ "tipo": "tutorias-vencidas", "clave": 57, "usuarioId": 17, "usuarioNombre": "Ramiro Molina", "fecha": "2026-11-11T16:22:29.0984897Z" }
```
`fecha` en **UTC** (sufijo `Z`; convertir a hora local en la UI). `usuarioNombre` = nombres + " " + apellidos de quien lo marcó.

**Tipos** (lista cerrada, sin distinguir mayúsculas; se devuelven en minúsculas) y qué id es la `clave`:

| `tipo` | `clave` | Fuente en la web |
|---|---|---|
| `tutorias-vencidas` | `idTutoria` | cálculo en cliente |
| `proximas-sin-estudiantes` | `idTutoria` | cálculo en cliente |
| `horarios-libres-7d` | `idHorario` | cálculo en cliente |
| `horarios-vencidos-disponibles` | `idHorario` | cálculo en cliente |
| `finalizadas-sin-calificacion` | `idTutoria` | cálculo en cliente |
| `encuestas-sin-responder` | `idTutoria` | `GET /Encuesta/pendientes` **[nuevo]** |

Regla para la UI: un asunto se muestra si **alguna** clave del cálculo actual no está en la lista de atendidos de su tipo (así, casos nuevos del mismo tipo lo vuelven a mostrar).

### EncuestasPendientes (item de GET /Encuesta/pendientes) **[nuevo]**
```json
{ "tutoriaId": 60, "tema": "La familia y la casa", "fechaHora": "2026-11-06T09:00:00", "enviadas": 1, "sinResponder": 1 }
```
Solo tutorías con `sinResponder > 0`; orden `fechaHora` descendente. `fechaHora` sin zona (hora local, como TutoriaResponse).

### HorarioCreate (POST/PUT /Horario)
| Campo | Tipo | Reglas |
|---|---|---|
| `titulo` | string \| null | **[cambio] opcional**, ≤ 100. Omitido, `null`, `""` o solo espacios → el servidor genera uno (ver abajo). Si viene con texto se guarda tal cual (recortando espacios de los extremos) |
| `fechaInicio`, `fechaFin`, `horaInicio`, `horaFin` | DateTime | obligatorios |
| `cupos` | string \| null | **[cambio 2026-11-21] OPCIONAL**: ausente, `null`, `""` o solo espacios → se guarda `""` (en POST y en PUT: un PUT sin `cupos` deja `""`). Si viene, debe ser un entero de 1 a 1000 (`" 05 "` → `"5"`); si no → 400 `Los cupos deben ser un número entero entre 1 y 1000`. Ya **no** define la capacidad de las tutorías nuevas (ver `TutoriaCreate.cupos`); solo sirve de respaldo para tutorías antiguas con `cupos` null. `HorarioResponse.cupos` sigue siendo string (`""` cuando no hay) |
| `espacio` | string \| null | **[cambio 2026-11-20] opcional** (aula), ≤ 60. `null`, ausente, `""` o solo espacios = **sin aula**: se guarda y se devuelve `""` (también `horarioEspacio: ""` en TutoriaResponse). Cada cliente decide el texto a mostrar; los correos y el Excel muestran "Por confirmar". En PUT, `null`/`""` quita el aula |
| `estado` | string | obligatorio, ≤ 60 (`Disponible`, `Libre`, `Ocupado`, `Cancelado` — texto libre, ver decisiones pendientes) |
| `usuarioId` | int | obligatorio, debe existir |

**Título generado** (determinista, ≤ 100 caracteres): `{espacio} · {dd/MM/yyyy} {HH:mm}–{HH:mm}`; si `fechaFin` es otro día: `{espacio} · {dd/MM/yyyy}–{dd/MM/yyyy} {HH:mm}–{HH:mm}`. Ej.: `Sala 3 · 15/03/2030 08:00–09:30`. **Sin aula**: sin el prefijo, p. ej. `15/03/2030 08:00–09:30`. Si el espacio es muy largo se recorta para no pasar de 100.
**PUT sin título**: si el título guardado es el que el servidor habría generado con los datos anteriores, se **regenera** con los nuevos (no queda desfasado al cambiar espacio/fecha/hora); si es un título escrito por alguien (web antigua, MAUI, E2E), se **conserva**. `HorarioResponse.titulo` siempre trae un string no vacío.

### HorarioResponse
```json
{
  "idHorario": 1, "titulo": "Tutoría Inglés", "fechaInicio": "2025-10-20T00:00:00", "fechaFin": "2025-10-20T00:00:00",
  "horaInicio": "2025-10-20T08:00:00", "horaFin": "2025-10-20T09:00:00", "cupos": "10", "espacio": "Sala A",
  "estado": "Disponible", "usuarioId": 2, "usuarioNombre": "Carlos", "usuarioApellidos": "Pérez", "usuarioCorreo": "carlos@..."
}
```
`usuarioId` **[+]**. `usuarioCorreo` ahora viene también en los listados (antes `null`) **[+]**. **[cambio, fase 4]** `usuarioId`, `usuarioNombre`, `usuarioApellidos`, `usuarioCorreo` son **`null`** si el dueño fue eliminado (el horario se conservó porque tiene tutorías). Ver [Datos desvinculados](#datos-desvinculados-usuario-eliminado-nuevo).

### TutoriaCreate (POST/PUT /Tutoria)
| Campo | Tipo | Reglas |
|---|---|---|
| `idioma`, `nivel`, `tema`, `modalidad` | string | obligatorios, ≤ 45 |
| `estado` | string | obligatorio, ≤ 45 (los clientes envían `"Pendiente"`) |
| `fechaTutoria` | DateTime (sin zona, hora local) | obligatorio. **[cambio, solo POST]** el día debe estar entre `fechaInicio` y `fechaFin` del horario; si trae hora, entre `horaInicio` (incl.) y `horaFin` (excl.) → si no, 400. Si llega con hora `00:00` se guarda con la `horaInicio` del horario |
| `usuarioId` | int | obligatorio: asignado. **[cambio, solo POST]** debe tener rol Docente o Tutor → si no, 400 `El usuario asignado debe ser Docente o Tutor` |
| `horarioId` | int | obligatorio. **[cambio, solo POST]** debe estar `Disponible`/`Libre` → si no, **409** `El horario ya no está disponible`. Al crear pasa a `Ocupado` (reserva atómica) |
| `estudianteIds` | int[] \| null | **[nuevo, opcional, solo POST]** estudiantes a asignar en la misma operación: todos o ninguno, respetando cupos (409). Ids repetidos se ignoran. En PUT se ignora |
| `cupos` | int \| null | **[nuevo 2026-11-21, BREAKING en POST]** máximo de estudiantes de la tutoría, entero 1–1000. **POST: obligatorio** → ausente/`null` → 400 `Los cupos son obligatorios`; fuera de rango → 400 `Los cupos deben ser un número entero entre 1 y 1000` (un valor no numérico como `"abc"` → 400 de validación del JSON). **PUT: opcional** (ausente/`null` = conserva); fuera de rango → 400; menor que los estudiantes ya inscritos → **409** `No se pueden fijar N cupos: la tutoría ya tiene M estudiantes inscritos` |

Campos extra (p. ej. `tutorId` que envía MAUI) se ignoran. Las validaciones marcadas "solo POST" no se aplican a `PUT /Tutoria/{id}` (gestión web, sin cambios).

### FinalizarTutoria (cuerpo OPCIONAL de PUT /Tutoria/{id}/finalizar) **[nuevo]**
| Campo | Tipo | Reglas |
|---|---|---|
| `calificacion` | decimal \| null | 1–5. Si viene, se crea la retroalimentación con **autor = usuario del token** en la misma transacción que la finalización |
| `comentario` | string \| null | ≤ 300; se guarda recortado (`""` si null). Si viene comentario sin `calificacion` → 400 |

Sin cuerpo (o `{}`) = comportamiento anterior (solo finaliza).

### TutoriaResponse
```json
{
  "idTutoria": 10, "idioma": "Inglés", "nivel": "A1", "tema": "Saludos", "modalidad": "Virtual",
  "estado": "Pendiente", "fechaTutoria": "2026-11-12T08:00:00",
  "usuarioId": 2, "horarioId": 1,
  "usuarioNombre": "Carlos", "usuarioApellidos": "Pérez", "usuarioCorreo": "carlos@...",
  "horarioEspacio": "Sala A", "horarioHoraInicio": "2026-11-12T08:00:00", "horarioHoraFin": "2026-11-12T09:00:00",
  "horarioUsuarioId": 8, "horarioUsuarioNombre": "Ana", "horarioUsuarioApellidos": "Martínez",
  "horarioTitulo": "Tutoría Inglés", "horarioCupos": 5, "cupos": 3,
  "estudiantes": [ /* UsuarioResponse */ ]
}
```
`usuarioId`, `horarioId` **[+]**. **[+] (fase 2)** `horarioUsuarioId` (dueño del horario = Docente que agendó; **[cambio, fase 4]** `null` si el dueño fue eliminado o no hay horario — antes `0`), `horarioUsuarioNombre`/`horarioUsuarioApellidos`, `horarioTitulo`, `horarioCupos` (`int` o `null` si el valor guardado no es un entero ≥ 1 = sin límite; se mantiene por compatibilidad). **[+] 2026-11-21** `cupos` (`int` o `null`): **capacidad efectiva** = `Tutoria.cupos`; si es null (tutorías antiguas) los cupos del horario; `null` = sin límite. **La UI debe usar `cupos`**. Cupos ocupados = `estudiantes.length`. `estudiantes` es siempre un array (en POST/PUT viene `[]`; antes `null`) **[cambio menor]**. Datos relacionados ausentes: `"N/A"` y `0001-01-01T00:00:00`. **[cambio, fase 4]** Asignado eliminado: `usuarioId`, `usuarioNombre`, `usuarioApellidos`, `usuarioCorreo` = `null` (tutoría **sin asignar**); dueño del horario eliminado: `horarioUsuarioId`, `horarioUsuarioNombre`, `horarioUsuarioApellidos` = `null`.

### RetroalimentacionCreate (POST/PUT /Retroalimentacion)
| Campo | Tipo | Reglas |
|---|---|---|
| `comentario` | string \| null | **opcional** **[cambio]** (antes obligatorio y `""` daba 400), ≤ 300. Se guarda `""` si viene null |
| `calificacion` | decimal | obligatorio, 1–5 |
| `tutoriaId`, `usuarioId` | int | obligatorios, deben existir. **[cambio, solo POST]** Docente/Tutor: `usuarioId` debe ser el suyo (403) y deben participar en la tutoría (403). **409** si ese usuario ya tiene una retroalimentación en esa tutoría (cualquier rol) |

### RetroalimentacionResponse
```json
{ "idRetroalimentacion": 7, "comentario": "", "calificacion": 4.5, "fecha": "2026-11-11T10:00:00",
  "usuarioId": 2, "tutoriaId": 10,
  "usuarioNombre": "Carlos", "usuarioApellidos": "Pérez", "tutoriaTema": "Saludos", "tutoriaIdioma": "Inglés", "tutoriaNivel": "A1" }
```
`usuarioId`, `tutoriaId` **[+]**. **[cambio, fase 4]** Autor eliminado: `usuarioId`, `usuarioNombre`, `usuarioApellidos` = `null`.

### Datos desvinculados (usuario eliminado) **[nuevo]**

Al eliminar un usuario sus datos **compartidos** se conservan con la referencia al usuario a `null`. Campos que ahora pueden ser `null` y cómo mostrarlos:

| Respuesta | Campos `null` | Mostrar en la UI |
|---|---|---|
| TutoriaResponse | `usuarioId`, `usuarioNombre`, `usuarioApellidos`, `usuarioCorreo` | "Sin asignar (usuario eliminado)". Si la tutoría está `Pendiente`/`Programada`, es un caso para **Requiere atención**: reasignar con `PUT /Tutoria/{id}` |
| TutoriaResponse | `horarioUsuarioId`, `horarioUsuarioNombre`, `horarioUsuarioApellidos` | "Usuario eliminado" |
| HorarioResponse | `usuarioId`, `usuarioNombre`, `usuarioApellidos`, `usuarioCorreo` | "Usuario eliminado" (el horario sigue editable con `PUT /Horario/{id}`, que exige un `usuarioId` válido) |
| RetroalimentacionResponse | `usuarioId`, `usuarioNombre`, `usuarioApellidos` | "Usuario eliminado" |
| AsuntoAtendido | `usuarioId`, `usuarioNombre` | "Atendido por un usuario eliminado" |
| EncuestaResponse | — (`estudianteNombre` = `"Usuario eliminado"`, `estudianteCorreo` = `eliminado-{idEncuesta}@usuario-eliminado.invalid`) | mostrar `estudianteNombre`; no usar ese correo |
| MetricasTutoriaDetallada | `tutorId` = `0`, `tutorNombre`/`tutorApellidos`/`tutorCorreo` = `"N/A"` (sin cambio de forma) | "Sin asignar" |

Regla general para la web: **un id de usuario `null` significa "usuario eliminado"**; no intentar navegar a su ficha. Con TypeScript: `usuarioId: number | null`, `usuarioNombre: string | null`, etc.

### EncuestaResponse
```json
{ "idEncuesta": 5, "calificacion": 0, "comentario": null, "fechaRespuesta": null,
  "estudianteNombre": "Jorge López", "estudianteCorreo": "jorge@...", "tokenUsado": false, "tutoriaId": 10 }
```
`comentario` puede ser `null`.

### EncuestaCreate (POST /Encuesta/responder)
| Campo | Tipo | Reglas |
|---|---|---|
| `token` | string | obligatorio |
| `calificacion` | int | obligatorio, 1–5 |
| `comentario` | string \| null | **opcional** **[cambio]** (antes `null` daba 400 implícito), ≤ 300 |

### NotificacionResponse
```json
{ "idNotificacion": 1, "fecha": "2026-11-11T08:00:00", "asunto": "Recordatorio", "descripcion": "...",
  "usuarioNombre": "Ana", "usuarioApellidos": "Martínez", "usuarioCorreo": "ana@..." }
```

### MetricasGenerales
```json
{ "totalTutorias": 5, "tutoriasCompletadas": 1, "tutoriasCanceladas": 1, "tutoriasProgramadas": 3,
  "promedioCalificacion": 4.54, "totalUsuarios": 5, "totalDocentes": 2, "totalEstudiantes": 1,
  "totalHorarios": 5, "horariosDisponibles": 3 }
```
**[cambio]** `tutoriasCompletadas` cuenta `Finalizada` + `Completada`; `tutoriasProgramadas` cuenta `Pendiente` + `Programada` + `Activo`; `horariosDisponibles` cuenta `Disponible` + `Libre` (sin distinguir mayúsculas). `totalDocentes` sigue contando solo rol `Docente`.

### MetricasTutoriaDetallada (item de GET /Metricas/tutorias)
```json
{ "idTutoria": 10, "idioma": "Inglés", "nivel": "A1", "tema": "Saludos", "modalidad": "Virtual", "estado": "Finalizada",
  "fechaTutoria": "...", "horarioEspacio": "Sala A", "horarioHoraInicio": "...", "horarioHoraFin": "...",
  "tutorId": 2, "tutorNombre": "Carlos", "tutorApellidos": "Pérez", "tutorCorreo": "...",
  "numeroAsistencias": 0, "totalEstudiantesAsignados": 2,
  "estudiantes": [ { "estudianteId": 5, "estudianteNombre": "...", "estudianteApellidos": "...", "estudianteCorreo": "...", "asistio": false } ] }
```

---

## Auth

| Método y ruta | Request | 200/201 | Errores propios |
|---|---|---|---|
| `POST /Auth/login` | `{ correo, contrasena }` | `{ message, token, tokenType, expiresAt, usuario }` **[cambio: + token, tokenType, expiresAt]** | 400, 401, 403, 429 |
| `GET /Auth/me` **[nuevo]** | — | UsuarioResponse | 401 |
| `PUT /Auth/contrasena` **[nuevo]** | CambiarContrasena | **204** sin cuerpo | 400 (validación, `La contraseña actual no es correcta`, nueva igual a la actual o al correo); 401 sin token / usuario borrado; 403 rol sin acceso; 429 (5 intentos / 15 min **por usuario**, `Retry-After`) |

### Restablecer contraseña ("¿Olvidaste tu contraseña?") **[nuevo]** — todos públicos

| Método y ruta | Request | Éxito | Errores |
|---|---|---|---|
| `POST /Auth/olvide-contrasena` | `{ "correo": "..." }` | **200** `{ "message": "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña." }` — SIEMPRE igual, exista o no la cuenta | 400 correo no válido (`message`); **429** + `Retry-After` (5 por IP y 5 por correo cada 15 min) |
| `GET /Auth/restablecer-contrasena/validar?token=...` | query `token` | **200** `{ "valido": true, "correo": "r***@cecar.edu.co", "expiraEn": "2026-11-20T06:16:02.98Z" }` (UTC) | 400 `{ message: "El enlace no es válido o ha caducado. Solicita uno nuevo." }`; 429 + `Retry-After` |
| `POST /Auth/restablecer-contrasena` | `{ "token": "...", "contrasenaNueva": "..." }` | **204** sin cuerpo | 400 token inválido/caducado/usado (mismo `message`) o política de contraseña (mensajes de `PUT /Auth/contrasena`); 429 + `Retry-After` (20 por IP / 15 min, compartido con validar) |

- Solo se envía correo a cuentas **activas con acceso** (Administrador, Auxiliar, Docente, Tutor). Estudiantes, inactivos e inexistentes: misma respuesta, nada enviado. El correo se envía en segundo plano (mismo tiempo de respuesta).
- Enlace: `{Web:UrlBase}/restablecer-contrasena?token=<43 caracteres base64url>`. Caduca a los 60 min (`RestablecimientoContrasena:MinutosValidez`), un solo uso; pedir uno nuevo invalida los anteriores.
- Política (mismos textos que `PUT /Auth/contrasena`): `La nueva contraseña debe tener entre 8 y 72 caracteres (máximo 72 bytes)`, `...no puede estar formada solo por espacios`, `La nueva contraseña debe ser distinta de la actual`, `La nueva contraseña no puede ser igual a su correo`. Si la política falla, el token sigue valiendo.
- Tras restablecer: los JWT emitidos antes devuelven **401** `La contraseña de la cuenta se cambió. Inicia sesión de nuevo`; se envía el aviso "Tu contraseña se cambió".
- Errores: los de negocio llevan solo `message` (y `detail`), **sin** `errors` por campo. Solo un JSON mal formado/tipo incorrecto produce `errors` con claves del binder (`"$.correo"`, `"$"`, `"dto"`), como en el resto de la API.
- **e2e (solo Development):** `POST /dev/restablecimiento/enlace` con token de **Administrador** y `{ "correo": "..." }` de una cuenta cuyo nombre empieza por `[PRUEBA]` (activa, con acceso) → 200 `{ "enlace", "token" }` válido, sin enviar correo. Otra cuenta → 404. Fuera de Development → 404.

### CambiarContrasena (PUT /Auth/contrasena)
| Campo | Tipo | Reglas |
|---|---|---|
| `contrasenaActual` | string | obligatorio; se verifica con BCrypt contra la guardada |
| `contrasenaNueva` | string | obligatorio; 8–72 caracteres (y ≤ 72 bytes UTF-8, límite de BCrypt); no solo espacios; distinta de la actual; distinta del correo (sin distinguir mayúsculas). Sin reglas de composición (NIST SP 800-63B) |

Actúa **solo** sobre el usuario del token (`sub`); campos extra como `idUsuario`/`correo` se ignoran. Contraseña actual incorrecta → **400** (no 401, para que el cliente no cierre la sesión). Los tokens emitidos antes del cambio **siguen siendo válidos** hasta su expiración (no hay revocación; ver propuestas).

Detalle en [autenticacion-y-autorizacion.md](autenticacion-y-autorizacion.md).

## Usuario

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `POST /Usuario` | UsuarioCreate | **201** UsuarioResponse, `Location: /api/Usuario/{id}` | 400; 403 (Auxiliar con rol Administrador/Auxiliar: `Solo un Administrador puede crear o modificar Administradores o Auxiliares`) **[cambio: el Auxiliar ya puede crear Docentes y Tutores]**; 404 rol inexistente; 409 correo duplicado |
| `GET /Usuario` | — | 200 UsuarioResponse[] (todos, sin cabecera) — **idéntico a antes si no se envía ningún parámetro** | — |
| `GET /Usuario?page=&pageSize=&q=&rol=&activo=` **[nuevo, aditivo]** | todos opcionales (ver abajo) | 200 UsuarioResponse[] **de la página** + cabecera `X-Total-Count` (total que cumple el filtro) | 400 (`page` < 1 o no entero; `pageSize` fuera de 1–100; `rol` desconocido; `q` > 100; `activo` no booleano) |
| `PATCH /Usuario/{id}/estado` **[nuevo]** | UsuarioEstado | 200 UsuarioResponse | 400 cuerpo; 403 Auxiliar sobre no Estudiante (`Solo un Administrador puede crear, modificar o eliminar usuarios que no sean Estudiantes`), Docente/Tutor; 404; **409** `No se puede desactivar al último Administrador activo` / `No puede desactivar su propia cuenta` |
| `GET /Usuario/{id}` | — | 200 UsuarioResponse | 404 |
| `GET /Usuario/by-rol/{tipoRol}?incluirInactivos=` | `tipoRol`: `Estudiante`, `Docente`, `Tutor`, ...; `incluirInactivos` bool opcional (por defecto `false`) | 200 UsuarioResponse[] — **[cambio] solo cuentas activas** salvo `incluirInactivos=true` | — |
| `PUT /Usuario/{id}` | UsuarioUpdate | 200 UsuarioResponse | 400; 403 (Auxiliar sobre/hacia Administrador o Auxiliar, mismo `message`) **[cambio: el Auxiliar ya puede editar Docentes y Tutores]**; 404; **409 correo de otro usuario [cambio]** |
| `DELETE /Usuario/{id}` **[cambio, fase 4]** | — | 200 `{ "message": "Usuario eliminado exitosamente", "resumen": ResultadoEliminacion }` — **eliminación definitiva**, ya no se bloquea por tutorías/horarios/retroalimentaciones | 400 `No puede eliminar su propio usuario`; 403 Auxiliar sobre no Estudiante, Docente/Tutor; 404; **409** `No se puede eliminar al último Administrador activo`. Ya **no** hay 409 "tiene registros asociados" |

**Eliminación de usuarios** (una transacción: todo o nada). Qué pasa con cada dependencia:

| Dependencia | Acción | Motivo |
|---|---|---|
| `TutoriaEstudiante` del usuario (sus inscripciones como estudiante) | se **borran** | dato propio; los compañeros y la tutoría no se tocan (los cupos quedan libres) |
| `Notificacion` del usuario | se **borran** | dato propio |
| Encuestas a su correo **sin responder** | se **borran** | invitación a una persona que ya no existe |
| Encuestas a su correo **respondidas** | se **conservan anonimizadas** (`estudianteNombre`=`Usuario eliminado`, correo marcador `.invalid`) | la calificación forma parte del historial de la tutoría |
| Horarios del usuario **sin tutorías** | se **borran** | no los usa nadie |
| Horarios del usuario **con tutorías** (de cualquier estado) | se **conservan sin dueño** (`usuarioId` null) | historial y tutorías de otros |
| Tutorías asignadas al usuario (finalizadas, canceladas o pendientes) | se **conservan sin asignar** (`usuarioId` null), mismo estado | tienen estudiantes, retroalimentaciones, encuestas. Las pendientes se reasignan con `PUT /Tutoria/{id}` |
| Retroalimentaciones e informes escritos por él | se **conservan sin autor** | historial de la tutoría |
| Asuntos atendidos que marcó | se **conservan sin autor** | el caso sigue atendido |
| Su JWT | deja de valer de inmediato (401) | `EstadoCuentaCache` se invalida |

**Nunca** se borra una tutoría ni datos de otro usuario. Reglas que se mantienen: nadie se elimina a sí mismo; el Auxiliar solo elimina Estudiantes; no se puede eliminar al último Administrador activo. Para retirar temporalmente a alguien sin perder nada, sigue existiendo `PATCH /Usuario/{id}/estado`.

`ResultadoEliminacion` (todos `int`): `inscripcionesEliminadas`, `notificacionesEliminadas`, `horariosEliminados`, `horariosConservados`, `tutoriasDesasignadas`, `retroalimentacionesConservadas`, `informesConservados`, `atendidosConservados`, `encuestasEliminadas`, `encuestasAnonimizadas`. Ejemplo (docente con tutorías finalizada, cancelada y pendiente):
```json
{ "message": "Usuario eliminado exitosamente",
  "resumen": { "inscripcionesEliminadas": 0, "notificacionesEliminadas": 2, "horariosEliminados": 1, "horariosConservados": 1,
               "tutoriasDesasignadas": 3, "retroalimentacionesConservadas": 1, "informesConservados": 1,
               "atendidosConservados": 0, "encuestasEliminadas": 0, "encuestasAnonimizadas": 0 } }
```
La UI puede mostrar el resumen ("3 tutorías quedaron sin asignar…") y debería pedir confirmación antes de llamar.

**Paginación y búsqueda de `GET /Usuario`** (se activa con **cualquiera** de los parámetros; `?q=` vacío no cuenta):
- `page` entero ≥ 1 (por defecto 1); `pageSize` 1–100 (por defecto **20**). Página más allá del total → `[]` con `X-Total-Count` correcto.
- `q`: texto ≤ 100. Se parte por espacios y **cada palabra** debe aparecer (subcadena) en `nombres`, `apellidos` o `correo`, sin distinguir mayúsculas **ni tildes** (`nunez` = `Núñez`, `JOSE` = `José`). `%`, `_` y `[` se buscan literalmente.
- `rol`: `Administrador|Auxiliar|Docente|Tutor|Estudiante` (sin distinguir mayúsculas). `activo`: `true|false`.
- Orden estable: `apellidos`, `nombres`, `idUsuario` (ascendente).
- `X-Total-Count` está en `Access-Control-Expose-Headers` (legible desde el navegador).

**Cuenta desactivada** (`activo: false`): login → **403** `La cuenta está desactivada. Contacte con un administrador` (solo si la contraseña es correcta; si no, 401 genérico). Cualquier petición con un token ya emitido de esa cuenta → **401** con el mismo `message` (inmediato en esta instancia; hasta 30 s si hubiera varias instancias de la API). No se le puede asignar una tutoría ni agregarla como estudiante (400). Reactivar con `PATCH ... { "activo": true }` restablece login y los tokens no caducados.

## Horario

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `POST /Horario` | HorarioCreate (`titulo` opcional **[cambio]**) | **201** HorarioResponse (con `titulo` generado si no se envió) | 400 (`cupos` presente pero no entero 1–1000 **[cambio]**; opcional desde 2026-11-21), 404 usuario |
| `GET /Horario` | — | 200 HorarioResponse[] | — |
| `GET /Horario/{id}` | — | 200 HorarioResponse | 404 |
| `GET /Horario/disponibles` | — | 200 HorarioResponse[] con estado `Disponible` o `Libre` **[cambio: + Libre]** | — |
| `GET /Horario/usuario/{usuarioId}` | — | 200 HorarioResponse[] | **403 si Docente/Tutor pide otro usuarioId [cambio]** |
| `PUT /Horario/{id}` | HorarioCreate | 200 HorarioResponse | 400 (`cupos` presente pero no entero 1–1000; opcional desde 2026-11-21), 404 |
| `DELETE /Horario/{id}` | — | 200 `{ "message": "Horario eliminado exitosamente" }` | 404; **409 tiene tutorías [cambio, antes 500]** |

## Tutoria

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `POST /Tutoria` | TutoriaCreate | **201** TutoriaResponse (`estudiantes` = los asignados en la misma operación, o `[]`) | 400 (validación, **`cupos` ausente o fuera de 1–1000 [nuevo 2026-11-21]**, fecha/hora fuera del horario, asignado no Docente/Tutor, estudiante no Estudiante, **asignado o estudiante con la cuenta desactivada [nuevo]**); **403** Tutor, o Docente en horario ajeno; 404 usuario/horario/estudiante; **409** horario no disponible o cupo superado (límite = `cupos` de la tutoría) **[cambio]** |
| `GET /Tutoria` | — | 200 TutoriaResponse[] | — |
| `GET /Tutoria/{id}` | — | 200 TutoriaResponse | 404; **403 sin acceso (regla de propiedad) [cambio]** |
| `PUT /Tutoria/{id}` | TutoriaCreate | 200 TutoriaResponse (`estudiantes: []`) | 400 (incl. **reasignar a una cuenta desactivada [nuevo]**; editar otros datos de una tutoría cuyo asignado ya está desactivado sí se permite; `cupos` fuera de 1–1000 [nuevo]), 404; **409** `cupos` menor que los inscritos **[nuevo 2026-11-21]** |
| `DELETE /Tutoria/{id}` | — | 200 `{ "message": "Tutoría eliminada exitosamente" }` | 404. **Borra también encuestas** (antes 500) **[cambio]** |
| `POST /Tutoria/{id}/estudiantes` | `{ "estudianteIds": [5, 6] }` (≥ 1) | 200 `{ "message": "Estudiantes agregados exitosamente" }` | 400 (usuario no Estudiante, **estudiante desactivado [nuevo]**, o todos ya asignados); 404 tutoría/estudiante; **403** sin acceso o Tutor (`El Tutor no puede agregar ni quitar estudiantes de una tutoría`); **409** `Cupo completo: X de Y ocupados (se intentaron agregar N)` **[cambio]**; Y = `cupos` de la tutoría, o los del horario si la tutoría es antigua (`cupos` null); sin límite si ninguno es numérico. Todo o nada |
| `GET /Tutoria/{id}/estudiantes` | — | 200 UsuarioResponse[] | 404; **403 sin acceso [cambio]** |
| `DELETE /Tutoria/{tutoriaId}/estudiantes/{estudianteId}` | — | 200 `{ "message": "Estudiante eliminado de la tutoría exitosamente" }` | 404; **403** sin acceso o Tutor. **[cambio aditivo]** antes solo Admin/Aux; ahora también el Docente participante |
| `GET /Tutoria/calendario/docente?nombre=&apellidos=&correo=` | query obligatorias | 200 TutoriaResponse[] | 400 faltan datos / no es docente-tutor; 404 |
| `GET /Tutoria/calendario/estudiante?nombre=&apellidos=&correo=` | query obligatorias | 200 TutoriaResponse[] | 400; 404 |
| `GET /Tutoria/calendario/idioma-nivel?idioma=&nivel=` | query obligatorias | 200 TutoriaResponse[] | 400 |
| `GET /Tutoria/estado/{estado}` | valor exacto guardado | 200 TutoriaResponse[] | — |
| `GET /Tutoria/estado/{estado}/usuario/{usuarioId}` | **grupo de estado [cambio]** (ver abajo) | 200 TutoriaResponse[] — tutorías **asignadas** al usuario | 400 usuarioId ≤ 0; 403 Docente/Tutor con otro usuarioId |
| `GET /Tutoria/agendadas/usuario/{usuarioId}?estado=` **[nuevo]** | `estado` opcional (grupo) | 200 TutoriaResponse[] — tutorías **agendadas en horarios del usuario** (`horarioUsuarioId == usuarioId`), aunque estén asignadas a otro; ordenadas por `fechaTutoria` | 400 usuarioId ≤ 0; 403 Docente/Tutor con otro usuarioId |
| `PUT /Tutoria/{id}/finalizar` | **cuerpo opcional** FinalizarTutoria **[cambio aditivo]** | 200 TutoriaResponse (`estado: "Finalizada"`) y envía encuestas | 400 ya finalizada (`Finalizada`/`Completada`) o cancelada, calificación fuera de 1–5, comentario sin calificación; 404; **403 sin acceso [cambio]**; **409** ya registró una retroalimentación (la tutoría NO se finaliza: transacción) |

**Grupos de estado** (sin distinguir mayúsculas) en `estado/{estado}/usuario/{id}` y `agendadas`: `pendiente`|`programada`|`activo` → los tres; `finalizada`|`completada` → los dos; cualquier otro valor (p. ej. `cancelada`) → solo ese. `GET /Tutoria/estado/{estado}` (gestión web) sigue comparando el valor exacto.

**Regla de propiedad** (403 `No tiene acceso a esta tutoría`): Admin/Aux → todas. Docente → tutorías asignadas a él **o** agendadas en un horario suyo: ver, finalizar, gestionar estudiantes. Tutor → solo asignadas: ver y finalizar; gestionar estudiantes → 403. Detalle en la [matriz](autenticacion-y-autorizacion.md#7-matriz-de-roles-por-endpoint).

## Retroalimentacion

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `POST /Retroalimentacion` | RetroalimentacionCreate | **201** RetroalimentacionResponse | 400, 404; **403** Docente/Tutor firmando por otro (`Solo puede registrar retroalimentaciones a su propio nombre`) o sin acceso a la tutoría; **409** ya existe una de ese usuario en esa tutoría **[cambio]** |
| `GET /Retroalimentacion` | — | 200 RetroalimentacionResponse[] | — |
| `GET /Retroalimentacion/{id}` | — | 200 RetroalimentacionResponse | 404 |
| `GET /Retroalimentacion/tutoria/{tutoriaId}` | — | 200 RetroalimentacionResponse[] | **[cambio aditivo]** ahora también Docente/Tutor participantes; 403 sin acceso, 404 tutoría inexistente (Admin/Aux: lista vacía, como antes) |
| `GET /Retroalimentacion/usuario/{usuarioId}` | — | 200 RetroalimentacionResponse[] | — |
| `PUT /Retroalimentacion/{id}` | RetroalimentacionCreate | 200 RetroalimentacionResponse | 400, 404 |
| `DELETE /Retroalimentacion/{id}` | — | 200 `{ "message": "Retroalimentación eliminada exitosamente" }` | 404 |

## Encuesta

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `POST /Encuesta/enviar/{tutoriaId}` | sin cuerpo | 200 `{ "message": "...", "enviadas": 0, "yaExistentes": 2 }` **[+ enviadas, yaExistentes; idempotente]** | 404; **403 sin acceso (misma regla que finalizar) [cambio]** |
| `GET /Encuesta/verificar/{token}` 🌐 | — | 200 EncuestaResponse | 404 token inválido; 400 ya respondida |
| `POST /Encuesta/responder` 🌐 | EncuestaCreate | 200 `{ "message": "¡Gracias por tu respuesta!", "encuesta": EncuestaResponse }` | 400 (validación o ya respondida); 404 |
| `GET /Encuesta/tutoria/{tutoriaId}` | — | 200 EncuestaResponse[] | — |
| `GET /Encuesta/estadisticas` | — | 200 `{ "totalEnviadas", "totalRespondidas", "promedioCalificacion", "tasaRespuesta" }` | — |
| `GET /Encuesta/pendientes` **[nuevo]** | — | 200 EncuestasPendientes[] | — |

Idempotencia de `enviar`: no se crea una segunda encuesta para el mismo correo de estudiante en la misma tutoría. `finalizar` ya envía las encuestas; llamar `enviar` después devuelve `enviadas: 0`.

Enlace del correo: `{Encuesta:UrlBase}?token={token}` (antes fijo a `http://localhost:5173/encuesta`).

## Metricas

| Método y ruta | Éxito |
|---|---|
| `GET /Metricas/generales` | 200 MetricasGenerales |
| `GET /Metricas/tutorias` | 200 MetricasTutoriaDetallada[] — **solo lectura [cambio]**: ya no inserta filas en `Informe` |

## Notificacion

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `GET /Notificacion` | — | 200 NotificacionResponse[] | — |
| `GET /Notificacion/{id}` | — | 200 NotificacionResponse | 404 |
| `GET /Notificacion/usuario/{usuarioId}` | — | 200 NotificacionResponse[] | — |
| `DELETE /Notificacion/{id}` | — | 200 `{ "message": "Notificación eliminada exitosamente" }` | 404 |
| `POST /Notificacion/email/cambio/{tutoriaId}` | cuerpo JSON string: `"Motivo del cambio"` | 200 `{ "message": "..." }` | 400 no se pudo enviar |
| `GET /Notificacion/reporte-excel/{tutoriaId}` | — | 200 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `Content-Disposition` (ahora expuesto por CORS) | 404 |

Los recordatorios por correo son automáticos (2 h antes) y ahora incluyen tutorías en estado `Pendiente` **[cambio]**.

## Atencion **[nuevo]** (asuntos atendidos del dashboard web)

Compartido por todos los Administradores y Auxiliares (cualquier equipo). Política `GestionWeb`. Esquema y tipos en [AsuntoAtendido](#asuntoatendido-respuesta-de-atencionatendidos-nuevo).

| Método y ruta | Request | Éxito | Errores propios |
|---|---|---|---|
| `GET /Atencion/atendidos?tipo=` | `tipo` opcional (sin él: todos los tipos) | 200 AsuntoAtendido[] (orden `tipo`, `clave`) | 400 tipo desconocido |
| `POST /Atencion/atendidos` | `{ "tipo": "horarios-libres-7d", "claves": [120, 121] }` (1–200 ids > 0; duplicados se ignoran) | 200 AsuntoAtendido[] — estado final de **todas** las claves enviadas | 400 (tipo desconocido, `claves` vacía/ausente, > 200, id ≤ 0) |
| `DELETE /Atencion/atendidos/{tipo}/{clave}` | — | **204** sin cuerpo (también si no existía) | 400 tipo desconocido |

- **Idempotente**: marcar una clave ya atendida no cambia quién ni cuándo (se conserva el primero). El usuario es el del token (no se acepta en el cuerpo).
- `DELETE` sirve para "deshacer" en la UI; cualquier Administrador/Auxiliar puede deshacer lo de otro.
- Los atendidos no caducan solos: si la tutoría/horario deja de estar en el cálculo, la fila simplemente no se usa.