using Microsoft.AspNetCore.Mvc;
using PSA.AppCore;
using PSA.EntidadesDTO.Entidades.Fincas;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FincaEvidenciasController : ControllerBase
    {
        private readonly FincaEvidenciaService _fincaEvidenciaService;
        private readonly IWebHostEnvironment _environment;

        public FincaEvidenciasController(
            FincaEvidenciaService fincaEvidenciaService,
            IWebHostEnvironment environment)
        {
            _fincaEvidenciaService = fincaEvidenciaService;
            _environment = environment;
        }

        [HttpPost("subir")]
        public async Task<IActionResult> Subir([FromForm] int idFinca, [FromForm] int cargadoPor, [FromForm] List<IFormFile> archivos)
        {
            try
            {
                if (idFinca <= 0)
                {
                    return BadRequest(new { Mensaje = "La finca es inválida." });
                }

                if (cargadoPor <= 0)
                {
                    return BadRequest(new { Mensaje = "El usuario cargadoPor es inválido." });
                }

                if (archivos == null || archivos.Count == 0)
                {
                    return BadRequest(new { Mensaje = "Debe adjuntar al menos un archivo." });
                }

                var carpetaBase = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", "fincas", idFinca.ToString());
                Directory.CreateDirectory(carpetaBase);

                var respuesta = new List<object>();

                foreach (var archivo in archivos)
                {
                    _fincaEvidenciaService.ValidarArchivo(archivo.FileName, archivo.Length);

                    var extension = Path.GetExtension(archivo.FileName);
                    var nombreGuardado = $"{Guid.NewGuid()}{extension}";
                    var rutaFisica = Path.Combine(carpetaBase, nombreGuardado);
                    var rutaRelativa = $"/uploads/fincas/{idFinca}/{nombreGuardado}";

                    await using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }

                    var evidencia = new FincaEvidencia
                    {
                        FincaId = idFinca,
                        NombreArchivo = archivo.FileName,
                        RutaArchivo = rutaRelativa,
                        TipoArchivo = archivo.ContentType ?? "application/octet-stream",
                        CargadoPor = cargadoPor
                    };

                    var idEvidencia = await _fincaEvidenciaService.CrearAsync(evidencia);

                    respuesta.Add(new
                    {
                        IdEvidencia = idEvidencia,
                        NombreArchivo = archivo.FileName
                    });
                }

                return Ok(new
                {
                    Mensaje = "Archivos cargados correctamente.",
                    Archivos = respuesta
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Mensaje = "No fue posible cargar los archivos.",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("por-finca/{idFinca:int}")]
        public async Task<IActionResult> ObtenerPorFinca(int idFinca)
        {
            if (idFinca <= 0)
            {
                return BadRequest(new { Mensaje = "El idFinca es inválido." });
            }

            var evidencias = await _fincaEvidenciaService.ObtenerPorFincaAsync(idFinca);
            foreach (var item in evidencias)
            {
                item.UrlDescarga = item.RutaArchivo;
            }

            return Ok(evidencias);
        }
    }
}