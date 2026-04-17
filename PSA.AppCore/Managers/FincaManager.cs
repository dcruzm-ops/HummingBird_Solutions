using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs;

namespace PSA.AppCore.Managers;

public class FincaManager
{
    private readonly FincaDAO _fincaDao;
    private readonly EvaluacionTecnicaManager _evaluacionTecnicaManager;

    public FincaManager(FincaDAO fincaDao, EvaluacionTecnicaManager evaluacionTecnicaManager)
    {
        _fincaDao = fincaDao;
        _evaluacionTecnicaManager = evaluacionTecnicaManager;
    }

    public Task<List<FincaResumenDTO>> ObtenerPorPropietarioAsync(int idPropietario) => _fincaDao.ObtenerPorPropietarioAsync(idPropietario);
    public Task<FincaDetalleDTO?> ObtenerDetalleAsync(int idFinca, int idPropietario) => _fincaDao.ObtenerDetalleAsync(idFinca, idPropietario);

    public async Task<int> RegistrarFincaAsync(RegistrarFincaDTO dto)
    {
        var idFinca = await _fincaDao.CrearFincaAsync(dto);
        await _evaluacionTecnicaManager.CrearPendientePorNuevaFincaAsync(idFinca);
        return idFinca;
    }

    public Task<bool> ActualizarFincaAsync(int idFinca, RegistrarFincaDTO dto) => _fincaDao.ActualizarFincaAsync(idFinca, dto);
    public Task<bool> EliminarFincaAsync(int idFinca, int idPropietario) => _fincaDao.EliminarFincaAsync(idFinca, idPropietario);
}
