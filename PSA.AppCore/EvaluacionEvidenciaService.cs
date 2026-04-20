using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.AppCore;

public class EvaluacionEvidenciaService(EvaluacionEvidenciaDAO evidenciaDao)
{
    private readonly EvaluacionEvidenciaDAO _evidenciaDao = evidenciaDao;

    public Task<int> CrearAsync(int idEvaluacion, string nombreArchivo, string rutaArchivo, string tipoArchivo, int cargadoPor)
        => _evidenciaDao.CrearAsync(idEvaluacion, nombreArchivo, rutaArchivo, tipoArchivo, cargadoPor);

    public Task<List<EvaluacionEvidenciaDTO>> ObtenerPorEvaluacionAsync(int idEvaluacion)
        => _evidenciaDao.ObtenerPorEvaluacionAsync(idEvaluacion);
}
