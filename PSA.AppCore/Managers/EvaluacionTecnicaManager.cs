using PSA.AppCore.Services.Notifications;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.AppCore.Managers
{
    public class EvaluacionTecnicaManager
    {
        private readonly EvaluacionTecnicaDAO _evaluacionTecnicaDAO;
        private readonly Services.IPaymentPlanService _paymentPlanService;
        private readonly INotificationDispatcher _notificationDispatcher;
        private readonly UsuarioDAO _usuarioDao;

        public EvaluacionTecnicaManager(
            EvaluacionTecnicaDAO evaluacionTecnicaDAO,
            Services.IPaymentPlanService paymentPlanService,
            INotificationDispatcher notificationDispatcher,
            UsuarioDAO usuarioDao)
        {
            _evaluacionTecnicaDAO = evaluacionTecnicaDAO ?? throw new ArgumentNullException(nameof(evaluacionTecnicaDAO));
            _paymentPlanService = paymentPlanService ?? throw new ArgumentNullException(nameof(paymentPlanService));
            _notificationDispatcher = notificationDispatcher ?? throw new ArgumentNullException(nameof(notificationDispatcher));
            _usuarioDao = usuarioDao ?? throw new ArgumentNullException(nameof(usuarioDao));
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
            => _evaluacionTecnicaDAO.ObtenerBandejaPendientesAsync();

        public Task<DetalleFincaParaEvaluacionDTO?> ObtenerDetalleAsync(int idEvaluacion)
        {
            if (idEvaluacion <= 0)
            {
                throw new InvalidOperationException("La evaluación es inválida.");
            }

            return _evaluacionTecnicaDAO.ObtenerDetalleParaEvaluacionAsync(idEvaluacion);
        }

        public async Task<bool> AsignarIngenieroAsync(int idEvaluacion, int idIngeniero)
        {
            if (idEvaluacion <= 0)
            {
                throw new InvalidOperationException("La evaluación es inválida.");
            }

            if (idIngeniero <= 0)
            {
                throw new InvalidOperationException("El ingeniero asignado es inválido.");
            }

            var asignado = await _evaluacionTecnicaDAO.AsignarIngenieroAsync(idEvaluacion, idIngeniero);
            if (!asignado)
            {
                return false;
            }

            var detalle = await _evaluacionTecnicaDAO.ObtenerDetalleParaEvaluacionAsync(idEvaluacion);
            if (detalle != null)
            {
                await _notificationDispatcher.NotifyInAppAsync(
                    detalle.IdPropietario,
                    "Evaluación en proceso",
                    $"La evaluación técnica de la finca \"{detalle.NombreFinca}\" fue tomada y está en proceso.",
                    NotificationCatalog.TipoInfo,
                    detalle.IdFinca);

                await _notificationDispatcher.NotifyInAppAsync(
                    idIngeniero,
                    "Evaluación asignada",
                    $"Se le asignó la evaluación #{idEvaluacion} de la finca \"{detalle.NombreFinca}\".",
                    NotificationCatalog.TipoSuccess,
                    idEvaluacion);
            }

            return true;
        }

        public async Task<bool> RegistrarResultadoAsync(int idEvaluacion, RegistrarResultadoEvaluacionDTO dto)
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
            if (dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase))
            {
                dto.DecisionTecnica = "Califica";
            }
            else if (dto.DecisionTecnica.Equals("No Califica", StringComparison.OrdinalIgnoreCase)
                || dto.DecisionTecnica.Equals("No califica", StringComparison.OrdinalIgnoreCase))
            {
                dto.DecisionTecnica = "No Califica";
            }
            else
            {
                throw new InvalidOperationException("La decisión técnica debe ser 'Califica' o 'No Califica'.");
            }

            dto.VegetacionAjustada = string.IsNullOrWhiteSpace(dto.VegetacionAjustada) ? null : dto.VegetacionAjustada.Trim();
            dto.UsoSueloAjustado = string.IsNullOrWhiteSpace(dto.UsoSueloAjustado) ? null : dto.UsoSueloAjustado.Trim();
            dto.PendienteAjustada = string.IsNullOrWhiteSpace(dto.PendienteAjustada) ? null : dto.PendienteAjustada.Trim();
            dto.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();

            var resultado = await _evaluacionTecnicaDAO.RegistrarResultadoAsync(idEvaluacion, dto);
            if (!resultado)
            {
                return false;
            }

            if (dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase))
            {
                await _paymentPlanService.GeneratePreliminaryPlanFromEvaluationAsync(
                    idEvaluacion,
                    DateTime.UtcNow.Year + 1,
                    actorId: null,
                    ip: null);
            }

            var detalle = await _evaluacionTecnicaDAO.ObtenerDetalleParaEvaluacionAsync(idEvaluacion);
            if (detalle != null)
            {
                var tituloEstado = dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase)
                    ? "Finca califica"
                    : "Finca no califica";
                var mensajeEstado = dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase)
                    ? $"La evaluación de \"{detalle.NombreFinca}\" finalizó y la finca califica para el programa."
                    : $"La evaluación de \"{detalle.NombreFinca}\" finalizó y la finca no califica para el programa.";

                await _notificationDispatcher.NotifyInAppAsync(
                    detalle.IdPropietario,
                    tituloEstado,
                    mensajeEstado,
                    dto.DecisionTecnica.Equals("Califica", StringComparison.OrdinalIgnoreCase) ? NotificationCatalog.TipoSuccess : NotificationCatalog.TipoWarning,
                    detalle.IdFinca);

                if (detalle.IdIngeniero.HasValue)
                {
                    await _notificationDispatcher.NotifyInAppAsync(
                        detalle.IdIngeniero.Value,
                        "Evaluación finalizada",
                        $"La evaluación #{idEvaluacion} de la finca \"{detalle.NombreFinca}\" se guardó y finalizó correctamente.",
                        NotificationCatalog.TipoSuccess,
                        idEvaluacion);
                }

                var propietario = await _usuarioDao.ObtenerPorIdAsync(detalle.IdPropietario);
                if (propietario != null)
                {
                    await _notificationDispatcher.NotifyEmailAsync(
                        propietario.Email,
                        $"Resultado de evaluación técnica - {detalle.NombreFinca}",
                        NotificationCatalog.EmailResultadoEvaluacion(
                            propietario.NombreCompleto,
                            detalle.NombreFinca,
                            dto.DecisionTecnica,
                            DateTime.UtcNow,
                            dto.Observaciones,
                            ConstruirResumenCambios(dto),
                            enlaceSistema: null));
                }
            }

            return true;
        }

        private static string? ConstruirResumenCambios(RegistrarResultadoEvaluacionDTO dto)
        {
            var cambios = new List<string>();

            if (dto.HectareasAjustadas.HasValue) cambios.Add($"Hectáreas ajustadas a {dto.HectareasAjustadas.Value:N2}");
            if (!string.IsNullOrWhiteSpace(dto.VegetacionAjustada)) cambios.Add($"Vegetación: {dto.VegetacionAjustada}");
            if (dto.RecursosHidricosAjustado.HasValue) cambios.Add($"Recursos hídricos: {(dto.RecursosHidricosAjustado.Value ? "Sí" : "No")}");
            if (!string.IsNullOrWhiteSpace(dto.UsoSueloAjustado)) cambios.Add($"Uso de suelo: {dto.UsoSueloAjustado}");
            if (!string.IsNullOrWhiteSpace(dto.PendienteAjustada)) cambios.Add($"Pendiente: {dto.PendienteAjustada}");

            return cambios.Count == 0 ? null : string.Join("; ", cambios);
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

        public Task<ReporteEvaluacionesDTO> ObtenerReporteEvaluacionesAsync(FiltroReporteEvaluacionesDTO filtro)
        {
            filtro ??= new FiltroReporteEvaluacionesDTO();

            if (filtro.Anio.HasValue && (filtro.Anio < 2000 || filtro.Anio > 2100))
            {
                throw new InvalidOperationException("El año del reporte no es válido.");
            }

            if (filtro.Mes.HasValue && (filtro.Mes < 1 || filtro.Mes > 12))
            {
                throw new InvalidOperationException("El mes del reporte no es válido.");
            }

            if (filtro.Anio == null && filtro.Mes.HasValue)
            {
                throw new InvalidOperationException("Para filtrar por mes debe indicar también el año.");
            }

            return _evaluacionTecnicaDAO.ObtenerReporteEvaluacionesAsync(filtro);
        }
    }
}
