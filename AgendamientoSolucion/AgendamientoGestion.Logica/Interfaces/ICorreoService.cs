using AgendamientoGestion.Logica.Correo;

namespace AgendamientoGestion.Logica.Interfaces
{
    /// <summary>Mensaje a enviar. Siempre con versión HTML y de texto plano.</summary>
    public sealed record MensajeCorreo(
        string Destinatario,
        string? NombreDestinatario,
        string Asunto,
        string Html,
        string Texto,
        string? RutaAdjunto = null)
    {
        public static MensajeCorreo Desde(CorreoRenderizado c, string destinatario, string? nombre = null) =>
            new(destinatario, nombre, c.Asunto, c.Html, c.Texto);
    }

    public interface ICorreoService
    {
        /// <summary>Envía el mensaje (multipart/alternative). Devuelve el Message-ID. Lanza excepción si no se pudo.</summary>
        string Enviar(MensajeCorreo mensaje);

        /// <summary>Compatibilidad: solo HTML; la versión de texto se deriva del HTML.</summary>
        void EnviarCorreo(string destinatario, string asunto, string htmlCuerpo, string? rutaAdjunto = null) =>
            Enviar(new MensajeCorreo(destinatario, null, asunto, htmlCuerpo, HtmlATexto.Convertir(htmlCuerpo), rutaAdjunto));

        void EnviarConAdjunto(string destino, string asunto, string contenidoHtml, string rutaAdjunto) =>
            EnviarCorreo(destino, asunto, contenidoHtml, rutaAdjunto);
    }
}
