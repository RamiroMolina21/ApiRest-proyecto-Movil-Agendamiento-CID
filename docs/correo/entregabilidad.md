# Entregabilidad del correo (por qué llega a spam y qué hacer)

Fecha: 2026-11-12. Ámbito: correos que envía la API (`AgendamientoGestion.Logica`). Ninguna configuración garantiza la
bandeja principal: Gmail, Outlook, etc. deciden con señales de autenticación, reputación del remitente, contenido y
comportamiento de los destinatarios. Lo que sigue reduce el riesgo; no lo elimina.

## 1. Situación actual

| Aspecto | Estado |
|---|---|
| Proveedor | `smtp.gmail.com:587` (STARTTLS) con una **cuenta personal @gmail.com** y contraseña de aplicación (user-secrets `SmtpSettings:User/Password`) |
| From | `TutorPro – CECAR <cuenta autenticada>` (desde 2026-11-12; antes la dirección sin nombre) |
| Volumen | bajo (avisos de tutorías, recordatorios, encuestas) |
| Enlaces del cuerpo | encuesta → `Encuesta:UrlBase` = `http://localhost:5173/encuesta` (desarrollo) |

### Qué hace Google con SPF/DKIM/DMARC en este caso
Al enviar por `smtp.gmail.com` autenticado con una cuenta @gmail.com, Google firma el mensaje con **DKIM de gmail.com**, el
**SPF** pasa (la IP es de Google, autorizada por `_spf.google.com`) y **DMARC de gmail.com** queda alineado porque el From es
`@gmail.com`. Es decir: la autenticación **ya pasa** y **no hay registros DNS que podamos crear** (el dominio gmail.com no es nuestro).

### Por qué aun así puede ir a spam
1. **Remitente personal para correo automático institucional**: un @gmail.com que envía avisos con aspecto de plataforma
   ("CECAR", botones, plantillas) es un patrón típico de suplantación; no hay dominio propio que acumule reputación.
2. **Reputación nueva / poco volumen** de esa cuenta para correo automatizado, y destinatarios que nunca le escribieron.
3. **Contenido** (antes de esta fase): solo HTML sin parte de texto, `mailto:` a un dominio inventado
   (`sistema@universidad.edu.co`), emojis en el cuerpo, enlaces a `localhost` (no resuelven fuera del equipo: enlace "roto"
   y sospechoso), exclamaciones.
4. **Pruebas a direcciones inexistentes** (`@cecar.test`) generan rebotes contra la cuenta remitente, lo que perjudica su reputación.
5. Si un destinatario marca un correo como spam, Gmail lo aprende para ese remitente.

## 2. Qué se corrigió en código (2026-11-12)

| Cambio | Archivo |
|---|---|
| **MailKit** en lugar de `System.Net.Mail.SmtpClient` (Microsoft no lo recomienda para desarrollo nuevo y remite a MailKit). Dependencia: MailKit 4.18.2 (MimeKit incluido); `dotnet list package --vulnerable` sin hallazgos | `Services/CorreoService.cs` |
| `multipart/alternative`: **texto plano + HTML**, ambos UTF-8; cabeceras con caracteres no ASCII codificadas (RFC 2047) | `CorreoService.ConstruirMensaje` |
| From con **nombre visible coherente** `TutorPro – CECAR` (`SmtpSettings:NombreRemitente`) y dirección = **cuenta autenticada** (aviso al arrancar si `Remitente` ≠ `User`) | `Models/SmtpSettings.cs`, `Program.cs` |
| **Reply-To** configurable (`SmtpSettings:ResponderA`); los botones `mailto:` usan esa dirección (antes un dominio inventado) | plantillas |
| **Message-ID** con el dominio del remitente y **Date** siempre presentes; `Auto-Submitted: auto-generated` (RFC 3834) y `X-Auto-Response-Suppress: All` contra respuestas automáticas | `CorreoService.ConstruirMensaje` |
| Asunto en una sola línea (sin inyección de cabeceras) y sin exclamaciones; "¡No olvides…!" → "No olvides…" | modelos/plantillas |
| **HTML-encoding automático** de todas las variables y protección contra inyección de plantilla | `Correo/MotorPlantillas.cs` |
| TLS obligatorio (`EnableSsl`: STARTTLS en 587, TLS implícito en 465; nunca se degrada a texto plano), **timeout** (30 s) y **reintentos** (2, con espera 2 s / 5 s) solo ante errores transitorios (4xx SMTP, red); los 5xx y errores de autenticación no se reintentan | `CorreoService.Enviar` |
| Registro: Message-ID, dominio del destinatario y respuesta SMTP; sin datos personales ni contraseñas | `CorreoService.Enviar` |
| Aviso al arrancar si `Encuesta:UrlBase` no es una URL pública **https** | `Program.cs` |

**Envío dentro de la petición (decisión):** se mantiene síncrono con timeout y reintentos. Así se conserva la semántica
actual ("la fila `Notificacion` solo se guarda si el correo salió") y el volumen es mínimo. Si el volumen crece o la latencia
molesta, la evolución correcta es una **bandeja de salida (outbox)** en BD + `BackgroundService` que envíe y reintente, no un
`Task.Run` sin persistencia (se perderían correos al reiniciar).

Verificación real (2026-11-12): un correo `nueva-tutoria` de ejemplo enviado a la dirección de pruebas autorizada. Datos del
lado del envío: `From: TutorPro – CECAR <cuenta@gmail.com>`, `Subject: Nueva Tutoría Asignada - Inglés B1`,
`Message-ID: <...@gmail.com>`, partes `text/plain; charset=utf-8` + `text/html; charset=utf-8`, respuesta del servidor
`2.0.0 OK ... - gsmtp` (aceptado) al primer intento. La clasificación en la bandeja del destinatario **no se puede observar desde el
servidor**: revisarla en Gmail ("Mostrar original" → SPF/DKIM/DMARC = PASS) y, si cae en spam, marcar "No es spam".

## 3. Qué requiere configuración externa (no se arregla con código)

### Límites de Gmail
- Cuenta **personal**: del orden de **500 destinatarios por 24 h** (rotativo) y 100 destinatarios por mensaje vía SMTP; al
  superarlo Google bloquea el envío temporalmente (`550 5.4.5 Daily sending quota exceeded`).
  Fuente: ayuda de Gmail "Límites de envío" (support.google.com/mail/answer/22839).
- **Google Workspace**: 2.000 mensajes/usuario/día, 10.000 destinatarios totales, 3.000 externos (cuentas de prueba: 500).
  Fuente: knowledge.workspace.google.com — "Gmail sending limits in Google Workspace".

### Recomendación para producción
Enviar desde un **dominio propio de la institución** (p. ej. `notificaciones@tutorias.cecar.edu.co`), por una de estas vías:

1. **Google Workspace** del dominio (cuenta de servicio o buzón `notificaciones@`), SMTP `smtp.gmail.com:587` o SMTP relay.
2. **Proveedor transaccional** (Amazon SES, Postmark, SendGrid, Mailgun, Brevo…): mejor para volumen, rebotes y estadísticas.
   Se usa igual: `SmtpSettings:Host/Port/User/Password` del proveedor; el código no cambia.

Registros DNS a crear en la zona del dominio (ejemplo con `cecar.edu.co` y Google Workspace; con un proveedor, sus valores):

| Registro | Nombre | Valor | Para qué |
|---|---|---|---|
| SPF (TXT) | `cecar.edu.co` (o el subdominio que envía) | `v=spf1 include:_spf.google.com ~all` — **un solo** registro SPF; si ya existe, añadir el `include:` al existente | autoriza los servidores que envían |
| DKIM (TXT) | `google._domainkey.cecar.edu.co` | la clave pública que genera la consola de Google Workspace (Aplicaciones → Gmail → Autenticar correo), **2048 bits**; después pulsar "Iniciar autenticación" | firma criptográfica |
| DMARC (TXT) | `_dmarc.cecar.edu.co` | empezar con `v=DMARC1; p=none; rua=mailto:dmarc@cecar.edu.co; adkim=r; aspf=r` y, tras revisar informes 2–4 semanas, pasar a `p=quarantine` y luego `p=reject` | política y alineación del From |
| PTR | — | lo gestiona el proveedor (Google/SES…) | DNS inverso de la IP |

Configuración de la API en producción (user-secrets / variables de entorno, nunca en appsettings):
```
SmtpSettings__Remitente=notificaciones@cecar.edu.co
SmtpSettings__User=notificaciones@cecar.edu.co
SmtpSettings__Password=<contraseña de aplicación o credencial SMTP del proveedor>
SmtpSettings__NombreRemitente=TutorPro – CECAR
SmtpSettings__ResponderA=coordinacion.idiomas@cecar.edu.co
Encuesta__UrlBase=https://<dominio público de la web>/encuesta
```

### Cómo verificar
- `nslookup -type=txt cecar.edu.co`, `nslookup -type=txt google._domainkey.cecar.edu.co`, `nslookup -type=txt _dmarc.cecar.edu.co`.
- Gmail → abrir un correo recibido → ⋮ → **Mostrar original**: SPF, DKIM y DMARC deben indicar **PASS** y el dominio del From.
- Herramientas: Google Admin Toolbox *Check MX*, **Google Postmaster Tools** (reputación del dominio y tasa de spam; Google
  pide mantenerla < 0,3 %, idealmente < 0,1 %), mail-tester.com (puntuación del mensaje).

### Buenas prácticas operativas
- No enviar pruebas a direcciones inexistentes desde la cuenta real (generan rebotes): usar el envío de prueba con lista
  blanca (`POST /dev/correos/{plantilla}/enviar`, ver `plantillas-e-inventario.md`) o un buzón de pruebas.
- Mantener `Encuesta:UrlBase` en https y en el mismo dominio institucional que el remitente.
- Si se envía a más de 5.000 destinatarios/día a Gmail (no es el caso), Google exige además DMARC alineado y baja con un clic
  (`List-Unsubscribe` + `List-Unsubscribe-Post`) para correo de marketing; los avisos transaccionales no lo requieren.
- Nombre visible solo identifica al remitente (sin asuntos ni emojis): "TutorPro – CECAR".

Fuentes: Google "Email sender guidelines" (support.google.com/a/answer/81126); Microsoft Learn, `System.Net.Mail.SmtpClient`
(observaciones: no recomendado para desarrollo nuevo, usar MailKit); Google Workspace "Gmail sending limits".
