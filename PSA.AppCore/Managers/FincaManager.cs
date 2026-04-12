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

            return idFinca;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo registrar la finca: {ex.Message}", ex);
        }
    }

    public Task<bool> ActualizarFincaAsync(int idFinca, RegistrarFincaDTO dto) => _fincaDao.ActualizarFincaAsync(idFinca, dto);
    public Task<bool> EliminarFincaAsync(int idFinca, int idPropietario) => _fincaDao.EliminarFincaAsync(idFinca, idPropietario);
}
