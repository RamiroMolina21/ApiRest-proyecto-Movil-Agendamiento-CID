# Correos: inventario y arquitectura de plantillas

Fecha: 2026-11-12. Para el rediseño visual (Alistar): **solo hace falta tocar los archivos de
`AgendamientoSolucion/AgendamientoGestion.Logica/Correo/Plantillas/`**. La lógica de negocio construye un modelo tipado y no
contiene HTML.

## 1. Inventario de correos que EXISTEN hoy

Rutas relativas a `AgendamientoSolucion/`. "Estudiantes" = los inscritos en la tutoría (`TutoriaEstudiante`) con rol Estudiante.

| # | Correo (plantilla) | Lo dispara | Destinatario (rol) | Asunto | Variables | Enlaces / botones | Código |
|---|---|---|---|---|---|---|---|
| 1 | Nueva tutoría asignada (`nueva-tutoria`) | `POST /api/Tutoria` (Admin/Aux/Docente) | Estudiantes de la tutoría; **si no tiene estudiantes, el Docente/Tutor asignado** | `Nueva Tutoría Asignada - {idioma} {nivel}` | destinatarioNombre, idTutoria, idioma, nivel, tema, modalidad, fechaHora, espacio | `mailto:` "Confirmar asistencia" y "Cancelar" a la dirección de contacto (Reply-To, o el From) | `Logica/Services/TutoriaService.cs:136` → `NotificacionService.EnviarNotificacionTutoriaPorEmailAsync` (`NotificacionService.cs:346`, plantilla en :408) |
| 2 | Nueva tutoría asignada (misma plantilla) | `POST /api/Tutoria/{id}/estudiantes` | Cada estudiante **agregado** | igual que 1 | igual que 1 | igual que 1 | `TutoriaService.cs:334` → `NotificacionService.EnviarNotificacionEstudianteTutoriaAsync` (`:735`, plantilla en :753) |
| 3 | Cambio en tutoría (`cambio-tutoria`) | `PUT /api/Tutoria/{id}` (motivo fijo "Cambio realizado en la tutoría") y `POST /api/Notificacion/email/cambio/{tutoriaId}` (motivo enviado por el cliente) | Estudiantes de la tutoría | `Cambio en Tutoría - {idioma} {nivel}` | destinatarioNombre, idTutoria, fechaHora, idioma, nivel, tema, modalidad, espacio, profesor, motivo, cambios[] (fecha, hora, tutor, aula, tema, idioma, nivel, modalidad), hayCambios | ninguno | `TutoriaService.cs:269`, `Agendamiento/Controllers/NotificacionController.cs:56` → `NotificacionService.EnviarNotificacionCambioPorEmailAsync` (`:490`, plantilla en :598) |
| 4 | Recordatorio (`recordatorio-tutoria`) | Automático: `RecordatorioTutoriaBackgroundService` cada minuto, tutorías Pendiente/Programada/Activo que empiezan en las **próximas 2 h**; una vez por tutoría cada 24 h | Estudiantes de la tutoría | `Recordatorio de Tutoría - {dd/MM/yyyy HH:mm}` | destinatarioNombre, fechaHora, idioma, nivel, tema, modalidad, espacio | ninguno | `Agendamiento/Services/RecordatorioTutoriaBackgroundService.cs:89` → `NotificacionService.EnviarRecordatorioPorEmailAsync` (`:433`, plantilla en :476) |
| 5 | Encuesta de satisfacción (`encuesta-satisfaccion`) | `PUT /api/Tutoria/{id}/finalizar` y `POST /api/Encuesta/enviar/{tutoriaId}` (idempotente: no repite a quien ya la tiene) | Estudiantes de la tutoría | `Encuesta de satisfacción - Tutoría` | destinatarioNombre (solo nombres), idioma, nivel, tema, enlace | botón "Responder encuesta" + enlace en texto: `{Encuesta:UrlBase}?token=...` (token de un solo uso) | `TutoriaService.cs:550`, `Agendamiento/Controllers/EncuestaController.cs:32` → `EncuestaService.EnviarEncuestasAsync` (`EncuestaService.cs:44`, envío en :94) |

Todos (salvo la encuesta) guardan además una fila en `Notificacion` **solo si el correo salió**.

### Existe la plantilla pero NO se envía
| Correo | Estado |
|---|---|
| Cancelación de tutoría (`cancelacion-tutoria`) | Plantilla conservada (`EmailTemplateService.GenerarTemplateCancelacion`), pero **ningún flujo la llama**: cancelar una tutoría es hoy un `PUT /Tutoria/{id}` con `estado = "Cancelada"`, que envía el correo de **cambio** (nº 3). Propuesta: detectar el paso a Cancelada y enviar esta plantilla |
| `NotificacionService.EnviarNotificacionTutoriaAsync` / `EnviarRecordatorioAsync` / `EnviarNotificacionCambioAsync` | Solo crean filas `Notificacion`, **no envían correo**, y ningún endpoint los usa (código heredado) |

### Correos que NO existen (propuestas, no implementadas)
| Pedido / caso | Estado |
|---|---|
| Confirmación de registro / bienvenida al crear usuario | **No existe** (`POST /api/Usuario` no envía nada) |
| Recuperación de contraseña ("olvidé mi contraseña") | **No existe** (solo `PUT /api/Auth/contrasena` con la contraseña actual). Requiere token de un solo uso con caducidad → cambio de esquema |
| Aviso de cambio de contraseña | No existe |
| Cuenta desactivada / reactivada / eliminada | No existe |
| Confirmación de asistencia (respuesta del estudiante) | No existe: los botones `mailto:` solo abren un correo al buzón de contacto |
| Notificaciones a Docentes/Tutores (nueva tutoría asignada a ellos, cambio, recordatorio, encuesta respondida) | No existen, salvo el caso "tutoría sin estudiantes" del nº 1 |
| Notificaciones administrativas (resúmenes, "requiere atención", tutorías sin asignar tras eliminar a un usuario) | No existen |

## 2. Arquitectura de plantillas

```
AgendamientoGestion.Logica/Correo/
  Plantillas/_layout.html|.txt      ← estructura común (encabezado, contenedor, pie)
  Plantillas/{tipo}.html|.txt       ← cuerpo de cada correo (5 tipos)
  Modelos.cs                        ← un record por tipo: variables, asunto, título, color, preencabezado
  MotorPlantillas.cs                ← motor mínimo tipo Mustache (sin dependencias)
  PlantillasCorreo.cs               ← IPlantillasCorreo.Renderizar(modelo) → CorreoRenderizado(Asunto, Html, Texto)
  EjemplosCorreo.cs                 ← datos de ejemplo para previsualizar
```
Las plantillas son **recursos incrustados** (`.csproj`: `EmbeddedResource ... LogicalName="correo/..."`): al cambiarlas hay
que recompilar.

**Sintaxis** (todo lo demás es HTML/texto normal):
- `{{variable}}` → valor **escapado** (HTML-encoding en `.html`; tal cual en `.txt`). Usar siempre esta forma.
- `{{#variable}} … {{/variable}}` → bloque si el valor tiene contenido; si es una lista, se repite (dentro, `{{.}}` es el elemento).
- `{{^variable}} … {{/variable}}` → bloque si está vacío.
- `{{! comentario }}`.
- `{{{contenido}}}` solo existe en el layout (cuerpo ya renderizado). Para cualquier otra variable sin escapar el motor lanza error.
- Una variable que no existe en el modelo es un **error** (lo detectan los tests).

Variables del **layout**: `titulo`, `colorEncabezado`, `preencabezado` (texto oculto que los clientes muestran junto al
asunto), `nombreSistema`, `contactoCorreo`, `anio`, `contenido`. Variables de cada tipo: tabla del inventario.
`contactoCorreo` también está disponible en las plantillas de cuerpo (null si SMTP no está configurado).

**Texto plano**: cada tipo tiene su `.txt` (se envía como `text/plain` junto al HTML, multipart/alternative). Mantener ambos
con el mismo contenido.

**Para el diseño (recomendaciones de entregabilidad)**: CSS en línea o en `<style>` del `<head>` (Gmail admite ambos; en
línea es lo más compatible), ancho ≤ 600 px, sin imágenes externas imprescindibles (muchos clientes las bloquean), sin
emojis que imiten gráficos (directriz de Google; hoy hay emojis heredados en títulos y botones), sin textos ocultos aparte
del preencabezado, enlaces visibles y del mismo dominio que el remitente, sin exclamaciones ni mayúsculas en el asunto.
Contradicción heredada a resolver en el diseño: el pie dice "no responder directamente" y la nueva tutoría pide "confirma tu
asistencia respondiendo a este correo".

**Qué toca cada uno**: Alistar → `Plantillas/*.html|.txt` (y, si quiere, `Titulo`/`ColorEncabezado`/`Preencabezado` en
`Modelos.cs`). Una variable nueva o un correo nuevo → pedirlo a backend (modelo + datos + disparador).

## 3. Previsualización (sin enviar correos)

1. **Navegador (API en Development)**: arrancar la API y abrir `http://localhost:5208/dev/correos` → lista de plantillas con
   enlaces a la versión HTML y texto (`/dev/correos/{plantilla}` y `?formato=texto`). Usa `EjemplosCorreo`. Fuera de
   Development responde 404.
2. **Archivos**: `dotnet test AgendamientoSolucion/Agendamiento.Tests --filter PlantillasCorreoTests` escribe
   `Agendamiento.Tests/bin/Debug/net8.0/correos-preview/{plantilla}.html|.txt` y comprueba que cada plantilla renderiza,
   escapa variables, no deja `{{…}}` y que toda plantilla tiene ejemplo.
3. **Envío real de prueba** (solo Development, solo Administrador, solo a direcciones de una lista blanca):
   `POST /dev/correos/{plantilla}/enviar` con `{ "para": "buzon@ejemplo.com" }`, arrancando la API con
   `CorreoPruebas__DestinatariosPermitidos__0=buzon@ejemplo.com`. Devuelve Message-ID, From y asunto. Sin la lista → 403.
