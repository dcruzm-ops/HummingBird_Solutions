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

    public Task<List<ItemFincaDuenoReporteDTO>> ObtenerReporteFincasDuenoAsync(int idPropietario)
        => _reportesDao.ObtenerReporteFincasDuenoAsync(idPropietario);

    public Task<List<ItemFincaPendienteIngenieroDTO>> ObtenerFincasPendientesIngenieroAsync()
        => _reportesDao.ObtenerFincasPendientesIngenieroAsync();

    public Task<List<ItemTecnicoFincaDTO>> ObtenerReporteTecnicoPorFincaAsync(int? idIngeniero, FiltroReporteDTO filtro)
        => _reportesDao.ObtenerReporteTecnicoPorFincaAsync(idIngeniero, filtro ?? new FiltroReporteDTO());

    public Task<List<ItemUsuarioRolReporteDTO>> ObtenerReporteUsuariosRolesAsync()
        => _reportesDao.ObtenerReporteUsuariosRolesAsync();

    public Task<List<ItemFincaEstadoAdminDTO>> ObtenerReporteFincasPorEstadoAsync()
        => _reportesDao.ObtenerReporteFincasPorEstadoAsync();

    public Task<List<ItemEvaluacionAdminDTO>> ObtenerReporteEvaluacionesAdminAsync(FiltroReporteDTO filtro)
        => _reportesDao.ObtenerReporteEvaluacionesAdminAsync(filtro ?? new FiltroReporteDTO());

    public Task<List<ItemPagosAdminDTO>> ObtenerReportePagosAdminAsync(int? anio)
        => _reportesDao.ObtenerReportePagosAdminAsync(anio);

    public Task<List<ItemAuditoriaCriticaDTO>> ObtenerAuditoriaCriticaAsync(int top = 50)
        => _reportesDao.ObtenerAuditoriaCriticaAsync(top);
}
