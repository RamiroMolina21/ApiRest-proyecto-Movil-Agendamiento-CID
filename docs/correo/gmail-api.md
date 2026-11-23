# Enviar los correos con la API de Gmail (para Render gratuito)

## Por qué

Desde septiembre de 2025, los servicios web **gratuitos** de Render bloquean el tráfico saliente a los puertos SMTP
25, 465 y 587 ([aviso de Render](https://render.com/changelog/free-web-services-will-no-longer-allow-outbound-traffic-to-smtp-ports)).
Por eso, en producción los correos no salen por SMTP. La API de Gmail envía desde la **misma cuenta de Gmail**, pero por
HTTPS (puerto 443, que no está bloqueado). Es gratis.

En tu equipo puedes seguir usando SMTP (es lo que viene por defecto). En Render se activa la API de Gmail con una variable.

Necesitas conseguir **tres datos** de Google (una sola vez): **Client ID**, **Client secret** y **Refresh token**.
Tardas unos 20 minutos. Usa siempre la cuenta **carlosleder2003@gmail.com**.

> El Client secret y el Refresh token son como una contraseña: no los pegues en el código, ni en chats, ni en capturas.
> Solo van en las variables de entorno de Render (y, si quieres probar en local, en user-secrets).

---

## Paso 1. Crear un proyecto en Google Cloud

1. Entra en <https://console.cloud.google.com/> con carlosleder2003@gmail.com (acepta los términos si es la primera vez).
2. Arriba, junto al logo, abre el selector de proyectos → **Proyecto nuevo**.
3. Nombre: `CDITutorPro Correo` (el que quieras) → **Crear**. Espera unos segundos y selecciona ese proyecto.

## Paso 2. Habilitar la API de Gmail

1. Menú (☰) → **APIs y servicios** → **Biblioteca**.
2. Busca **Gmail API** → ábrela → **Habilitar**.

## Paso 3. Pantalla de consentimiento OAuth

Guía oficial: [Configure the OAuth consent screen](https://developers.google.com/workspace/guides/configure-oauth-consent).

1. Menú (☰) → **Google Auth Platform** → **Branding** (en español puede aparecer como "Desarrollo de la marca").
   Si dice "Google Auth Platform not configured yet", pulsa **Get started / Comenzar**.
2. **Nombre de la app**: `CDITutorPro`. **Correo de asistencia**: carlosleder2003@gmail.com → **Siguiente**.
3. **Público (Audience)**: elige **Externo (External)** → **Siguiente**.
4. **Información de contacto**: carlosleder2003@gmail.com → **Siguiente**.
5. Marca la casilla de la política de datos de usuario de Google → **Continuar** → **Crear**.
6. Ve a **Audience → Test users → Add users**, añade **carlosleder2003@gmail.com** y guarda.
7. Ve a **Data Access (Acceso a los datos) → Add or remove scopes**. En "Añadir alcances manualmente" pega:
   `https://www.googleapis.com/auth/gmail.send` → **Añadir a la tabla** → **Actualizar** → **Guardar**.
   (Este permiso solo deja **enviar** correos; no puede leer tu bandeja. Google lo clasifica como "sensible":
   [lista de alcances de Gmail](https://developers.google.com/gmail/api/auth/scopes).)

### MUY IMPORTANTE: publicar la app

Mientras la app esté en estado **Testing (Prueba)**, Google hace que el refresh token **caduque a los 7 días**, y los
correos dejarían de salir cada semana
([Google: caducidad del refresh token](https://developers.google.com/identity/protocols/oauth2#expiration)).

1. Ve a **Google Auth Platform → Audience**.
2. En "Publishing status" pulsa **Publish app (Publicar app)** → confirma. Debe quedar **In production (En producción)**
   ([Google: estado de publicación](https://support.google.com/cloud/answer/15549945)).
3. Google puede avisar de que la app necesita verificación. **Para uso propio no hace falta verificarla**. Solo verás
   una advertencia ("Google no ha verificado esta app") cuando autorices en el paso 5, y podrás aceptarla.

Haz esto **ANTES** del paso 5. Si sacaste el refresh token estando en Testing, repite el paso 5 después de publicar.

## Paso 4. Crear la credencial OAuth (Client ID y Client secret)

1. **Google Auth Platform → Clients (Clientes)** → **Create client (Crear cliente)**
   (o bien **APIs y servicios → Credenciales → Crear credenciales → ID de cliente de OAuth**).
2. **Tipo de aplicación**: **Aplicación web**. Nombre: `CDITutorPro API`.
3. En **URIs de redireccionamiento autorizados** → **Añadir URI** y pega exactamente:
   `https://developers.google.com/oauthplayground`
4. **Crear**. Copia el **Client ID** (termina en `.apps.googleusercontent.com`) y el **Client secret**, y guárdalos en un
   lugar seguro (por ejemplo, tu gestor de contraseñas). Google ya no vuelve a mostrar el secret completo después:
   si lo pierdes, crea uno nuevo en ese mismo cliente.

## Paso 5. Obtener el refresh token con OAuth 2.0 Playground

1. Abre <https://developers.google.com/oauthplayground>.
2. Arriba a la derecha pulsa el **engranaje (⚙ OAuth 2.0 configuration)**:
   - marca **Use your own OAuth credentials**;
   - pega tu **OAuth Client ID** y tu **OAuth Client secret**;
   - deja "Access type: Offline" → cierra el panel.
3. A la izquierda, en "Step 1", en el cuadro **Input your own scopes** escribe:
   `https://www.googleapis.com/auth/gmail.send` → **Authorize APIs**.
4. Inicia sesión con **carlosleder2003@gmail.com**. Si aparece "Google no ha verificado esta app":
   **Configuración avanzada → Ir a CDITutorPro (no seguro)**. Es tu propia app. Acepta el permiso **Enviar correo
   electrónico en tu nombre**.
5. Vuelves al Playground ("Step 2"): pulsa **Exchange authorization code for tokens**.
6. Copia el valor **Refresh token** (empieza normalmente por `1//`). Ese es el tercer dato.
7. (Recomendado) Vuelve al engranaje y **desmarca** "Use your own OAuth credentials" para que el Playground no guarde tus datos.

El refresh token deja de funcionar si: cambias la contraseña de la cuenta (con permisos de Gmail), revocas el acceso en
<https://myaccount.google.com/permissions>, no se usa durante 6 meses o la app sigue en Testing (7 días). En ese caso
la API registra en el log **"refresh token revocado o caducado (invalid_grant)"**; repite el paso 5 y actualiza la
variable en Render.

## Paso 6. Configurar Render

En el panel de Render → tu servicio web → **Environment** → añade:

| Variable | Valor |
|---|---|
| `Correo__Proveedor` | `GmailApi` |
| `GmailApi__ClientId` | el Client ID del paso 4 |
| `GmailApi__ClientSecret` | el Client secret del paso 4 |
| `GmailApi__RefreshToken` | el refresh token del paso 5 |
| `SmtpSettings__Remitente` | `carlosleder2003@gmail.com` (el From; **debe ser esta cuenta** o un alias "Enviar como" verificado en ella) |
| `SmtpSettings__ResponderA` | opcional, como hasta ahora (buzón para las respuestas) |

- Ya **no hacen falta** `SmtpSettings__User`, `SmtpSettings__Password`, `SmtpSettings__Host` ni `SmtpSettings__Port`
  en Render (puedes borrarlas). Si `Remitente` está vacío se usa `SmtpSettings__User`, así que también vale dejar
  `User` con la dirección y quitar solo la contraseña.
- Guarda → Render redespliega. En el log de arranque debe aparecer `Proveedor de correo: GmailApi`.
  Si falta algún dato, **la API no arranca** y el log dice exactamente qué variable falta (Render mantiene la versión
  anterior en marcha si el nuevo despliegue falla).
- Prueba: usa "¿Olvidaste tu contraseña?" con tu cuenta y comprueba que llega el correo. En Gmail, el mensaje aparece
  también en **Enviados** de carlosleder2003@gmail.com.

## Probar en local (opcional)

En la carpeta `AgendamientoSolucion/Agendamiento`:

```
dotnet user-secrets set "Correo:Proveedor" "GmailApi"
dotnet user-secrets set "GmailApi:ClientId" "<client id>"
dotnet user-secrets set "GmailApi:ClientSecret" "<client secret>"
dotnet user-secrets set "GmailApi:RefreshToken" "<refresh token>"
dotnet user-secrets set "CorreoPruebas:DestinatariosPermitidos:0" "<tu correo>"
```

Arranca la API (Development), inicia sesión como Administrador y llama a
`POST /dev/correos/{plantilla}/enviar` con `{ "para": "<tu correo>" }`. La respuesta incluye `"proveedor": "GmailApi"`
y el `messageId`. Solo envía a direcciones de la lista blanca. Para volver a SMTP:
`dotnet user-secrets remove "Correo:Proveedor"`.

## Detalles técnicos (para desarrolladores)

- `Correo:Proveedor`: `Smtp` (por defecto) | `GmailApi`. Otro valor → la API no arranca.
- Token: `POST https://oauth2.googleapis.com/token` (`grant_type=refresh_token`), cacheado en memoria (singleton,
  thread-safe) hasta 2 min antes de `expires_in`. Envío: `POST https://gmail.googleapis.com/gmail/v1/users/me/messages/send`
  con `{"raw": <MIME RFC 5322 en base64url>}`
  ([Google: enviar correo](https://developers.google.com/workspace/gmail/api/guides/sending)). El MIME es el mismo que el
  SMTP (`CorreoService.ConstruirMensaje`).
- Errores: 401 → renueva el token y reintenta una vez; 429/5xx/red → hasta `GmailApi:Reintentos` (2) con espera 2 s/5 s/10 s;
  `invalid_grant` → error sin reintento con log claro; otros 4xx → error sin reintento. Solo cuenta como enviado un 200
  con `id`; si no, lanza excepción y la `Notificacion` no se guarda (igual que con SMTP). Un fallo nunca rompe la
  operación de negocio (crear tutoría, etc.). Los secretos y tokens nunca se registran en logs.
- Cuotas: la API de Gmail usa la misma cuenta y sus límites diarios de envío de Gmail.
