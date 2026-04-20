using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;
using PSA.AppCore.Services.Notifications;

namespace PSA.AppCore.Managers;

public class FincaManager
{
    private readonly FincaDAO _fincaDao;
    private readonly EvaluacionTecnicaManager _evaluacionTecnicaManager;
    private readonly INotificationDispatcher _notificationDispatcher;

    public FincaManager(FincaDAO fincaDao, EvaluacionTecnicaManager evaluacionTecnicaManager, INotificationDispatcher notificationDispatcher)
    {
        _fincaDao = fincaDao;
        _evaluacionTecnicaManager = evaluacionTecnicaManager;
        _notificationDispatcher = notificationDispatcher;
    }

    public Task<List<FincaResumenDTO>> ObtenerPorPropietarioAsync(int idPropietario) => _fincaDao.ObtenerPorPropietarioAsync(idPropietario);
    public Task<FincaDetalleDTO?> ObtenerDetalleAsync(int idFinca, int idPropietario) => _fincaDao.ObtenerDetalleAsync(idFinca, idPropietario);

    public async Task<int> RegistrarFincaAsync(RegistrarFincaDTO dto)
    {
        try
        {
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

    public Task<bool> ActualizarFincaAsync(int idFinca, RegistrarFincaDTO dto) => _fincaDao.ActualizarFincaAsync(idFinca, dto);
    public Task<bool> EliminarFincaAsync(int idFinca, int idPropietario) => _fincaDao.EliminarFincaAsync(idFinca, idPropietario);

    public async Task<int> GenerarRenovacionAnualAsync(int idFinca, int idPropietario, string? ipOrigen)
    {
        var detalle = await _fincaDao.ObtenerDetalleAsync(idFinca, idPropietario);
        if (detalle == null)
        {
            throw new InvalidOperationException("La finca no existe o no pertenece al propietario autenticado.");
        }

        var idEvaluacion = await _evaluacionTecnicaManager.CrearPendientePorNuevaFincaAsync(idFinca);
        await _notificationDispatcher.NotifyInAppAsync(
            idPropietario,
            "Renovación anual creada",
            $"Se creó la renovación anual de la finca \"{detalle.NombreFinca}\" y quedó en cola técnica.",
            NotificationCatalog.TipoInfo,
            idFinca);

        return idEvaluacion;
    }
}
