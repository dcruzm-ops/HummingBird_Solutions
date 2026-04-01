using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.Entidades.Evaluaciones;
using PSA.EntidadesDTO.DTOs.Fincas;

namespace PSA.AppCore
{
    public class EvaluacionService
    {
        private readonly EvaluacionDAO _evaluacionDAO;
        private readonly FincaDAO _fincaDAO;

        public EvaluacionService(EvaluacionDAO evaluacionDAO, FincaDAO fincaDAO)
        {
            _evaluacionDAO = evaluacionDAO;
            _fincaDAO = fincaDAO;
        }

        public async Task<int> CrearEvaluacionAsync(EvaluacionTecnica evaluacion)
        {
            if (evaluacion == null)
                throw new Exception("La evaluación es requerida.");

            evaluacion.Estado = "Pendiente";
            evaluacion.Decision = "Pendiente";

            var id = await _evaluacionDAO.CrearEvaluacionAsync(evaluacion);

            var finca = _fincaDAO.RetrieveById(evaluacion.FincaId);

            if (finca != null)
            {
                finca.EstadoFinca = "EnRevision";
                _fincaDAO.Update(finca);
            }

            return id;
        }

        public async Task FinalizarEvaluacionAsync(int evaluacionId, string decision, string? observaciones)
        {
            if (evaluacionId <= 0)
                throw new Exception("El id de la evaluación es inválido.");

            if (string.IsNullOrWhiteSpace(decision))
                throw new Exception("La decisión es obligatoria.");

            var evaluacion = await _evaluacionDAO.ObtenerPorIdAsync(evaluacionId);

            if (evaluacion == null)
                throw new Exception("Evaluación no encontrada.");

            if (evaluacion.Estado == "Finalizada")
                throw new Exception("La evaluación ya fue finalizada.");

            evaluacion.Decision = decision switch
            {
                "Aprobada" => "Califica",
                "Rechazada" => "No Califica",
                "Suspendida" => "No Califica",
                _ => throw new Exception("Decisión inválida")
            };
            evaluacion.Estado = "Finalizada";
            evaluacion.Observaciones = observaciones;

            await _evaluacionDAO.ActualizarEvaluacionAsync(evaluacion);

            var finca = _fincaDAO.RetrieveById(evaluacion.FincaId);

            if (finca != null)
            {
                finca.EstadoFinca = decision switch
                {
                    "Aprobada" => "Aprobada",
                    "Rechazada" => "Rechazada",
                    "Suspendida" => "Inactiva", 
                    _ => "EnRevision"
                };

                _fincaDAO.Update(finca);
            }
        }
    }
}
