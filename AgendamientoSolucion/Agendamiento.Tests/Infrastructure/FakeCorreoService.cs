using AgendamientoGestion.Logica.Interfaces;
using System.Collections.Concurrent;

namespace Agendamiento.Tests.Infrastructure;

/// <summary>Sustituye al SMTP real: registra los correos en memoria (nunca envía nada).</summary>
public sealed class FakeCorreoService : ICorreoService
{
    public sealed record Correo(string Destinatario, string Asunto, string Html, string Texto);

    public ConcurrentQueue<Correo> Enviados { get; } = new();

    /// <summary>Destinatarios para los que el envío falla (simula un buzón rechazado / SMTP caído para uno).</summary>
    public ConcurrentDictionary<string, bool> FallarPara { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string Enviar(MensajeCorreo m)
    {
        if (FallarPara.ContainsKey(m.Destinatario))
            throw new InvalidOperationException($"Fallo SMTP simulado para {m.Destinatario}");
        Enviados.Enqueue(new Correo(m.Destinatario, m.Asunto, m.Html, m.Texto));
        return $"<{Guid.NewGuid():N}@pruebas.local>";
    }
}
