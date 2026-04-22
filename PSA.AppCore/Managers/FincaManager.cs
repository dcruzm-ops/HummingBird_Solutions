using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.AppCore.Services.Notifications;
using PSA.EntidadesDTO.DTOs.Fincas;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Managers;

public class FincaManager
{
    private readonly FincaDAO _fincaDao;
    private readonly EvaluacionTecnicaManager _evaluacionTecnicaManager;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly AuditoriaLogDAO _auditoriaLogDao;

    public FincaManager(
        FincaDAO fincaDao,
        EvaluacionTecnicaManager evaluacionTecnicaManager,
        INotificationDispatcher notificationDispatcher,
        AuditoriaLogDAO auditoriaLogDao)
    {
        _fincaDao = fincaDao;
        _evaluacionTecnicaManager = evaluacionTecnicaManager;
        _notificationDispatcher = notificationDispatcher;
        _auditoriaLogDao = auditoriaLogDao;
    }

    public Task<List<FincaResumenDTO>> ObtenerPorPropietarioAsync(int idPropietario) => _fincaDao.ObtenerPorPropietarioAsync(idPropietario);
    public Task<FincaDetalleDTO?> ObtenerDetalleAsync(int idFinca, int idPropietario) => _fincaDao.ObtenerDetalleAsync(idFinca, idPropietario);

    public async Task<EstadoRenovacionAnualDTO> ObtenerEstadoRenovacionAnualAsync(int idFinca, int idPropietario)
    {
        var detalle = await _fincaDao.ObtenerDetalleAsync(idFinca, idPropietario);
        if (detalle is null)
        {
            throw new InvalidOperationException("La finca no existe o no pertenece al propietario autenticado.");
        }

        var elegibilidad = await _fincaDao.ObtenerElegibilidadRenovacionAsync(idFinca, idPropietario)
            ?? throw new InvalidOperationException("No fue posible verificar la elegibilidad de renovación.");

        var estadoExpiradoOVencido = string.Equals(elegibilidad.EstadoPlanActual, EstadosPlanPago.Finalizado, StringComparison.OrdinalIgnoreCase)
                                     || string.Equals(elegibilidad.EstadoPlanActual, EstadosPlanPago.Cancelado, StringComparison.OrdinalIgnoreCase);
        var faltaUnaCuota = elegibilidad.TienePlanVigente && elegibilidad.CuotasRestantes == 1;
        var cumpleCondicionPago = estadoExpiradoOVencido || faltaUnaCuota;
        var puedeRenovar = cumpleCondicionPago && !elegibilidad.ExisteRenovacionPendienteMismoCiclo;

        var motivo = puedeRenovar
            ? "Cumple criterios para solicitar renovación anual."
            : elegibilidad.ExisteRenovacionPendienteMismoCiclo
                ? "Ya existe una evaluación técnica pendiente activa para esta finca."
                : "Solo se permite renovar cuando el plan actual está vencido/expirado o cuando falta una única cuota para completarlo.";

        return new EstadoRenovacionAnualDTO
        {
            PuedeRenovar = puedeRenovar,
            Motivo = motivo,
            EstadoPlanActual = elegibilidad.EstadoPlanActual,
            CuotasRestantes = elegibilidad.CuotasRestantes,
            ExisteEvaluacionPendienteActiva = elegibilidad.ExisteRenovacionPendienteMismoCiclo
        };
    }

    public async Task<int> RegistrarFincaAsync(RegistrarFincaDTO dto)
    {
        try
        {
            ValidarUbicacionAdministrativa(dto);
            var idFinca = await _fincaDao.CrearFincaAsync(dto);

            try
            {
                await _evaluacionTecnicaManager.CrearPendientePorNuevaFincaAsync(idFinca);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"No se pudo crear la evaluación técnica pendiente para la finca {idFinca}: {ex.Message}");
            }

            await _notificationDispatcher.NotifyInAppAsync(
                dto.IdPropietario,
                "Finca registrada",
                $"La finca \"{dto.NombreFinca}\" fue registrada y enviada al flujo de evaluación.",
                NotificationCatalog.TipoSuccess,
                idFinca);

            return idFinca;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo registrar la finca: {ex.Message}", ex);
        }
    }

    private static void ValidarUbicacionAdministrativa(RegistrarFincaDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Provincia)
            || string.IsNullOrWhiteSpace(dto.Canton)
            || string.IsNullOrWhiteSpace(dto.Distrito))
        {
            throw new InvalidOperationException("Provincia, cantón y distrito son obligatorios y deben venir resueltos en backend.");
        }

        var provincia = dto.Provincia.Trim();
        var canton = dto.Canton.Trim();
        var distrito = dto.Distrito.Trim();
        if (provincia.StartsWith("Provincia ", StringComparison.OrdinalIgnoreCase)
            || canton.StartsWith("Canton ", StringComparison.OrdinalIgnoreCase)
            || distrito.StartsWith("Distrito ", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ubicación administrativa no es válida. Verifique provincia/cantón/distrito.");
        }
    }

    public Task<bool> ActualizarFincaAsync(int idFinca, RegistrarFincaDTO dto) => _fincaDao.ActualizarFincaAsync(idFinca, dto);
    public Task<bool> EliminarFincaAsync(int idFinca, int idPropietario) => _fincaDao.EliminarFincaAsync(idFinca, idPropietario);

    public async Task<int> GenerarRenovacionAnualAsync(int idFinca, int idPropietario, string? ipOrigen)
    {
        var detalle = await _fincaDao.ObtenerDetalleAsync(idFinca, idPropietario);
        if (detalle == null)
        {
            throw new InvalidOperationException("La finca no existe o no pertenece al propietario autenticado.");
        }

        var estadoRenovacion = await ObtenerEstadoRenovacionAnualAsync(idFinca, idPropietario);
        if (!estadoRenovacion.PuedeRenovar)
        {
            throw new InvalidOperationException(estadoRenovacion.Motivo);
        }

        var idEvaluacion = await _evaluacionTecnicaManager.CrearPendientePorNuevaFincaAsync(idFinca);

        await _auditoriaLogDao.RegistrarEventoAsync(
            idPropietario,
            "Fincas",
            "EvaluacionesTecnicas",
            "RENOVACION_ANUAL_GENERADA",
            $"Renovación anual generada para finca #{idFinca}. IdEvaluacion={idEvaluacion}. EstadoPlan={estadoRenovacion.EstadoPlanActual}, CuotasRestantes={estadoRenovacion.CuotasRestantes}.",
            idEvaluacion,
            ipOrigen);

        await _notificationDispatcher.NotifyInAppAsync(
            idPropietario,
            "Renovación anual creada",
            $"Se creó la renovación anual de la finca \"{detalle.NombreFinca}\" y quedó en cola técnica.",
            NotificationCatalog.TipoInfo,
            idFinca);

        return idEvaluacion;
    }
}
