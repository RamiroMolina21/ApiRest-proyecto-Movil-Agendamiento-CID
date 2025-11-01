using AgendamientoGestion.Logica.Dtos;
using AgendamientoGestion.Logica.Interfaces;
using AgendamientoGestion.Logica.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agendamiento.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            try
            {
                var metricas = await _metricasService.GetMetricasGeneralesAsync();
                return Ok(metricas);
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("tutorias")]
        public async Task<IActionResult> GetMetricasTutorias()
        {
            try
            {
                var metricas = await _metricasService.GetMetricasTutoriasAsync();
                return Ok(metricas);
            }
            catch (BusinessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
