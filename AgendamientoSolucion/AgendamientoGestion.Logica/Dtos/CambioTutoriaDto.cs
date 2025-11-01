using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Dtos
{
    public class CambioTutoriaDto
    {
        public string CambioFecha { get; set; }
        public string CambioHora { get; set; }
        public string CambioTutor { get; set; }
        public string CambioAula { get; set; }
        public string CambioTema { get; set; }
        public string CambioIdioma { get; set; }
        public string CambioNivel { get; set; }
        public string CambioModalidad { get; set; }

        public bool TieneCambios()
        {
            return !string.IsNullOrEmpty(CambioFecha) ||
                   !string.IsNullOrEmpty(CambioHora) ||
                   !string.IsNullOrEmpty(CambioTutor) ||
                   !string.IsNullOrEmpty(CambioAula) ||
                   !string.IsNullOrEmpty(CambioTema) ||
                   !string.IsNullOrEmpty(CambioIdioma) ||
                   !string.IsNullOrEmpty(CambioNivel) ||
                   !string.IsNullOrEmpty(CambioModalidad);
        }

        public List<string> ObtenerListaCambios()
        {
            var cambios = new List<string>();
            if (!string.IsNullOrEmpty(CambioFecha)) cambios.Add(CambioFecha);
            if (!string.IsNullOrEmpty(CambioHora)) cambios.Add(CambioHora);
            if (!string.IsNullOrEmpty(CambioTutor)) cambios.Add(CambioTutor);
            if (!string.IsNullOrEmpty(CambioAula)) cambios.Add(CambioAula);
            if (!string.IsNullOrEmpty(CambioTema)) cambios.Add(CambioTema);
            if (!string.IsNullOrEmpty(CambioIdioma)) cambios.Add(CambioIdioma);
            if (!string.IsNullOrEmpty(CambioNivel)) cambios.Add(CambioNivel);
            if (!string.IsNullOrEmpty(CambioModalidad)) cambios.Add(CambioModalidad);
            return cambios;
        }
    }
}

