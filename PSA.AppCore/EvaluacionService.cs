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

            if (evaluacion.FincaId <= 0)
                throw new Exception("La finca es requerida.");

            if (evaluacion.IngenieroForestalId <= 0)
                throw new Exception("El ingeniero forestal es requerido.");

            if (evaluacion.FechaEvaluacion == default)
                throw new Exception("La fecha de evaluación es requerida.");

            if (string.IsNullOrWhiteSpace(evaluacion.Estado))
                throw new Exception("El estado de la evaluación es requerido.");

            if (evaluacion.Estado == "Finalizada")
            {
                if (string.IsNullOrWhiteSpace(evaluacion.Decision))
                    throw new Exception("La decisión es obligatoria cuando la evaluación está finalizada.");

                evaluacion.Decision = NormalizarDecision(evaluacion.Decision);
            }
            else
            {
                evaluacion.Decision = null;
            }

            var id = await _evaluacionDAO.CrearEvaluacionAsync(evaluacion);

            var finca = _fincaDAO.RetrieveById(evaluacion.FincaId);

            if (finca != null)
            {
                finca.EstadoFinca = evaluacion.Estado switch
                {
                    "Pendiente" => "Pendiente",
                    "En proceso" => "EnRevision",
                    "Finalizada" => evaluacion.Decision switch
                    {
                        "Califica" => "Aprobada",
                        "No Califica" => "Rechazada",
                        _ => finca.EstadoFinca
                    },
                    _ => finca.EstadoFinca
                };

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

            evaluacion.Decision = NormalizarDecision(decision);
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

        private static string NormalizarDecision(string decision)
        {
            return decision.Trim() switch
            {
                "Aprobada" => "Califica",
                "Rechazada" => "No Califica",
                "Suspendida" => "No Califica",
                "Califica" => "Califica",
                "No Califica" => "No Califica",
                _ => throw new Exception("La decisión técnica no es válida.")
            };
        }
    }
}