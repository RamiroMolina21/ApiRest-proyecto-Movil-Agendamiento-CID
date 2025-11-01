using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class RetroalimentacionResponseDto
    {
        public int IdRetroalimentacion { get; set; }
        public string Comentario { get; set; }
        public decimal Calificacion { get; set; }
        public DateTime Fecha { get; set; }
        public string UsuarioNombre { get; set; }
        public string UsuarioApellidos { get; set; }
        public string TutoriaTema { get; set; }
        public string TutoriaIdioma { get; set; }
        public string TutoriaNivel { get; set; }
    }
}
