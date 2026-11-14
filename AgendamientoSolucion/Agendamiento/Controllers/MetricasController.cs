using Agendamiento.Security;
using AgendamientoGestion.Logica.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Politicas.GestionWeb)]
    public class MetricasController : ControllerBase
    {
        private readonly IMetricasService _metricasService;

        public MetricasController(IMetricasService metricasService)
        {
            _metricasService = metricasService;
        }

        [HttpGet("generales")]
        public async Task<IActionResult> GetMetricasGenerales()
        {
            return Ok(await _metricasService.GetMetricasGeneralesAsync());
        }

        /// <summary>Solo lectura (ya no inserta filas en Informe).</summary>
        [HttpGet("tutorias")]
        public async Task<IActionResult> GetMetricasTutorias()
        {
            return Ok(await _metricasService.GetMetricasTutoriasAsync());
        }
    }
}
