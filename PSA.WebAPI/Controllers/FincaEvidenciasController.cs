using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PSA.AppCore;
using PSA.WebAPI.Extensions;

namespace PSA.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FincaEvidenciasController : ControllerBase
    {
        private readonly FincaEvidenciaService _fincaEvidenciaService;
        private readonly EvaluacionEvidenciaService _evaluacionEvidenciaService;
        private readonly IWebHostEnvironment _environment;

        public FincaEvidenciasController(FincaEvidenciaService fincaEvidenciaService, EvaluacionEvidenciaService evaluacionEvidenciaService, IWebHostEnvironment environment)
        {
            _fincaEvidenciaService = fincaEvidenciaService;
            _evaluacionEvidenciaService = evaluacionEvidenciaService;
            _environment = environment;
        }

        [HttpPost("subir")]
        [Authorize(Roles = "2")]
        public Task<IActionResult> SubirFinca([FromForm] int idFinca, [FromForm] List<IFormFile> archivos)
            => SubirCoreAsync(idFinca, null, archivos, this.GetUserId(), true);

        [HttpPost("evaluacion/{idEvaluacion:int}/subir")]
        [Authorize(Roles = "3")]
        public Task<IActionResult> SubirEvaluacion([FromRoute] int idEvaluacion, [FromForm] List<IFormFile> archivos)
            => SubirCoreAsync(0, idEvaluacion, archivos, this.GetUserId(), false);

        [HttpGet("finca/{idFinca:int}")]
        [Authorize(Roles = "2,3")]
        public async Task<IActionResult> ObtenerPorFinca(int idFinca)
        {
            var evidencias = await _fincaEvidenciaService.ObtenerPorFincaAsync(idFinca);
            evidencias.ForEach(x => x.UrlDescarga = x.RutaArchivo);
            return Ok(evidencias);
        }

        [HttpGet("por-finca/{idFinca:int}")]
        [Authorize(Roles = "2,3")]
        public Task<IActionResult> ObtenerPorFincaLegacy(int idFinca) => ObtenerPorFinca(idFinca);

        [HttpGet("evaluacion/{idEvaluacion:int}")]
        [Authorize(Roles = "3")]
        public async Task<IActionResult> ObtenerPorEvaluacion(int idEvaluacion)
            => Ok(await _evaluacionEvidenciaService.ObtenerPorEvaluacionAsync(idEvaluacion));

        private async Task<IActionResult> SubirCoreAsync(int idFinca, int? idEvaluacion, List<IFormFile> archivos, int actorId, bool esFinca)
        {
            if (archivos == null || archivos.Count == 0) return BadRequest(new { Mensaje = "Debe adjuntar al menos un archivo." });
            var targetId = esFinca ? idFinca : idEvaluacion.GetValueOrDefault();
            if (targetId <= 0) return BadRequest(new { Mensaje = "Identificador inválido." });

            var carpetaTipo = esFinca ? "fincas" : "evaluaciones";
            var carpetaBase = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads", carpetaTipo, targetId.ToString());
            Directory.CreateDirectory(carpetaBase);

            var respuesta = new List<object>();
            foreach (var archivo in archivos)
            {
                _fincaEvidenciaService.ValidarArchivo(archivo.FileName, archivo.Length);
                var nombreGuardado = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                var rutaFisica = Path.Combine(carpetaBase, nombreGuardado);
                var rutaRelativa = $"/uploads/{carpetaTipo}/{targetId}/{nombreGuardado}";
                await using var stream = new FileStream(rutaFisica, FileMode.Create);
                await archivo.CopyToAsync(stream);

                var id = esFinca
                    ? await _fincaEvidenciaService.CrearAsync(new PSA.EntidadesDTO.Entidades.Fincas.FincaEvidencia { FincaId = idFinca, NombreArchivo = archivo.FileName, RutaArchivo = rutaRelativa, TipoArchivo = archivo.ContentType ?? "application/octet-stream", CargadoPor = actorId })
                    : await _evaluacionEvidenciaService.CrearAsync(idEvaluacion!.Value, archivo.FileName, rutaRelativa, archivo.ContentType ?? "application/octet-stream", actorId);

                respuesta.Add(new { IdEvidencia = id, NombreArchivo = archivo.FileName });
            }

            return Ok(new { Mensaje = "Archivos cargados correctamente.", Archivos = respuesta });
        }
    }
}
