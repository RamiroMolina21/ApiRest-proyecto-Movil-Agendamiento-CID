using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Entidades.Entities;

public class Horario {
    public int idHorario { get; set; }
    public string titulo { get; set; }
    public DateTime fechaInicio { get; set; }
    public DateTime fechaFin { get; set; }
    public DateTime horaInicio { get; set; }
    public DateTime horaFin { get; set; }
    public string cupos { get; set; }
    public string espacio { get; set; }
    public string estado { get; set; }
    public int Usuario_idUsuario { get; set; }

    // Propiedades de navegación
    public Usuario Usuario { get; set; }
    public ICollection<Tutoria> Tutorias { get; set; }
}
