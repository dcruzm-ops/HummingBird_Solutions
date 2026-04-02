using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.AppCore.Managers
{
    public class EvaluacionTecnicaManager
    {
        private readonly EvaluacionTecnicaDAO _evaluacionTecnicaDAO;

        public EvaluacionTecnicaManager(EvaluacionTecnicaDAO evaluacionTecnicaDAO)
        {
            _evaluacionTecnicaDAO = evaluacionTecnicaDAO ?? throw new ArgumentNullException(nameof(evaluacionTecnicaDAO));
        }

        public Task<int> CrearPendientePorNuevaFincaAsync(int idFinca)
        {
            if (idFinca <= 0)
            {
                throw new InvalidOperationException("La finca es inválida para crear evaluación pendiente.");
            }

            return _evaluacionTecnicaDAO.CrearEvaluacionPendienteAsync(idFinca);
        }

        public Task<List<BandejaEvaluacionPendienteDTO>> ObtenerBandejaPendienteAsync()
        {
            return _evaluacionTecnicaDAO.ObtenerBandejaPendientesAsync();
        }

        public Task<DetalleFincaParaEvaluacionDTO?> ObtenerDetalleAsync(int idEvaluacion)
        {
            if (idEvaluacion <= 0)
            {
                throw new InvalidOperationException("La evaluación es inválida.");
            }

            return _evaluacionTecnicaDAO.ObtenerDetalleParaEvaluacionAsync(idEvaluacion);
        }

        public Task<bool> AsignarIngenieroAsync(int idEvaluacion, int idIngeniero)
        {
            if (idEvaluacion <= 0)
            {
                throw new InvalidOperationException("La evaluación es inválida.");
            }

            if (idIngeniero <= 0)
            {
                throw new InvalidOperationException("El ingeniero asignado es inválido.");
            }

            return _evaluacionTecnicaDAO.AsignarIngenieroAsync(idEvaluacion, idIngeniero);
        }

        public Task<bool> RegistrarResultadoAsync(int idEvaluacion, RegistrarResultadoEvaluacionDTO dto)
        {
            if (idEvaluacion <= 0)
            {
                throw new InvalidOperationException("La evaluación es inválida.");
            }

            if (dto == null)
            {
                throw new InvalidOperationException("Debe enviar los datos de resultado de evaluación.");
            }

            if (dto.FechaVisita == default)
            {
                throw new InvalidOperationException("La fecha de visita es obligatoria.");
            }

            if (string.IsNullOrWhiteSpace(dto.DecisionTecnica))
            {
                throw new InvalidOperationException("La decisión técnica es obligatoria.");
            }

            dto.DecisionTecnica = dto.DecisionTecnica.Trim();
            if (!dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase)
                && !dto.DecisionTecnica.Equals("No Califica", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La decisión técnica debe ser 'Califica' o 'No Califica'.");
            }

            return _evaluacionTecnicaDAO.RegistrarResultadoAsync(idEvaluacion, dto);
        }

        public Task<bool> AvanzarEstadoAsync(int idEvaluacion, string nuevoEstado)
        {
            if (idEvaluacion <= 0)
            {
                throw new InvalidOperationException("La evaluación es inválida.");
            }

            if (string.IsNullOrWhiteSpace(nuevoEstado))
            {
                throw new InvalidOperationException("El estado de evaluación es obligatorio.");
            }

            nuevoEstado = nuevoEstado.Trim();
            if (!EstadosEvaluacionTecnica.Todos.Contains(nuevoEstado))
            {
                throw new InvalidOperationException("El estado de evaluación no es válido para el flujo técnico.");
            }

            return _evaluacionTecnicaDAO.ActualizarEstadoEvaluacionAsync(idEvaluacion, nuevoEstado);
        }
    }
}
