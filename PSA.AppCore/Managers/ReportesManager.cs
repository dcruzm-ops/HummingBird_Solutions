using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Reportes;

namespace PSA.AppCore.Managers;

public class ReportesManager
{
    private readonly ReportesDAO _reportesDao;

    public ReportesManager(ReportesDAO reportesDao)
    {
        _reportesDao = reportesDao;
    }

    public Task<ReportePagosDuenoDTO> ObtenerPagosDuenoAsync(int idPropietario, FiltroReporteDTO filtro)
        => _reportesDao.ObtenerPagosDuenoAsync(idPropietario, filtro ?? new FiltroReporteDTO());

    public Task<List<ItemTransaccionDuenoDTO>> ObtenerTransaccionesDuenoAsync(int idPropietario, FiltroReporteDTO filtro)
        => _reportesDao.ObtenerTransaccionesDuenoAsync(idPropietario, filtro ?? new FiltroReporteDTO());

    public Task<ReporteEvaluacionesIngenieroDTO> ObtenerEvaluacionesIngenieroAsync(int idIngeniero, FiltroReporteDTO filtro)
        => _reportesDao.ObtenerEvaluacionesIngenieroAsync(idIngeniero, filtro ?? new FiltroReporteDTO());

    public Task<List<ItemPagoUbicacionDTO>> ObtenerPagosPorUbicacionAsync(FiltroReporteDTO filtro)
        => _reportesDao.ObtenerPagosPorUbicacionAsync(filtro ?? new FiltroReporteDTO());

    public Task<List<ItemResumenActividadDTO>> ObtenerResumenActividadAsync()
        => _reportesDao.ObtenerResumenActividadAsync();
}
