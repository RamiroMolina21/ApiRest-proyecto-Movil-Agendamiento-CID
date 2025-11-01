namespace AgendamientoGestion.Logica.Interfaces
{
    public interface ICorreoService
    {
        void EnviarCorreo(string destinatario, string asunto, string htmlCuerpo, string rutaAdjunto = null);
        void EnviarConAdjunto(string destino, string asunto, string contenidoHtml, string rutaAdjunto);
    }
}
