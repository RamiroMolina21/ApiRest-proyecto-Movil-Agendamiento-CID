using AgendamientoGestion.Logica.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgendamientoGestion.Logica.Interfaces
{
    public interface IMetricasService
    {
        Task<MetricasResponseDto> GetMetricasGeneralesAsync();
        Task<List<MetricasTutoriasDetalladasDto>> GetMetricasTutoriasAsync();
    }
}
