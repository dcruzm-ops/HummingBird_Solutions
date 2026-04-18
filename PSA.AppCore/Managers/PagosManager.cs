using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Managers;

public class PagosManager(PlanPagoDAO planPagoDao, AuditoriaLogDAO auditoriaLogDao)
{
    private readonly PlanPagoDAO _planPagoDao = planPagoDao;
    private readonly AuditoriaLogDAO _auditoriaLogDao = auditoriaLogDao;

    public async Task<PlanPagoDTO?> GenerarPlanPagoAsync(GenerarPlanPagoRequestDTO request, int idUsuario, string? ip)
    {
        if (request.IdFinca <= 0)
        {
            throw new InvalidOperationException("Debe indicar una finca válida.");
        }

        if (request.Anio < DateTime.UtcNow.Year)
        {
            throw new InvalidOperationException("El año del plan no puede ser anterior al año actual.");
        }

        var plan = await _planPagoDao.GenerarPlanPagoAsync(request);
        if (plan != null && !request.Simular)
        {
            await _auditoriaLogDao.RegistrarEventoAsync(
                idUsuario: idUsuario,
                modulo: "Pagos",
                tablaAfectada: "PlanesPago",
                accion: "GENERAR_PLAN_PAGO",
                detalle: $"Plan {plan.IdPlanPago} generado para finca {plan.IdFinca} año {plan.Anio}.",
                idRegistroAfectado: plan.IdPlanPago,
                ipOrigen: ip);
        }

        return plan;
    }

    public Task<List<CuotaPlanPagoDTO>> ObtenerHistorialDuenoAsync(int idPropietario)
    {
        if (idPropietario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un propietario válido.");
        }

        return _planPagoDao.ObtenerHistorialCuotasDuenoAsync(idPropietario);
    }

    public Task<List<PlanPagoResumenDTO>> ObtenerPlanesDuenoAsync(int idPropietario)
    {
        if (idPropietario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un propietario válido.");
        }

        return _planPagoDao.ObtenerPlanesDuenoAsync(idPropietario);
    }

    public Task<List<CuentaBancariaDuenoDTO>> ObtenerCuentasBancariasDuenoAsync(int idUsuario)
    {
        if (idUsuario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un usuario válido.");
        }

        return _planPagoDao.ObtenerCuentasBancariasDuenoAsync(idUsuario);
    }

    public Task<int> RegistrarCuentaBancariaDuenoAsync(RegistrarCuentaBancariaDTO dto)
    {
        if (dto.IdUsuario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un usuario válido.");
        }

        if (string.IsNullOrWhiteSpace(dto.Banco)
            || string.IsNullOrWhiteSpace(dto.NumeroCuenta)
            || string.IsNullOrWhiteSpace(dto.TipoCuenta)
            || string.IsNullOrWhiteSpace(dto.Titular))
        {
            throw new InvalidOperationException("Debe completar todos los datos de la cuenta bancaria.");
        }

        return _planPagoDao.RegistrarCuentaBancariaDuenoAsync(dto);
    }

    public Task<bool> AsociarCuentaPlanAsync(int idPlanPago, AsociarCuentaPlanDTO dto)
    {
        if (idPlanPago <= 0)
        {
            throw new InvalidOperationException("Debe indicar un plan válido.");
        }

        if (dto.IdUsuario <= 0 || dto.IdCuentaBancaria <= 0)
        {
            throw new InvalidOperationException("Debe indicar usuario y cuenta bancaria válidos.");
        }

        return _planPagoDao.AsociarCuentaPlanAsync(idPlanPago, dto.IdUsuario, dto.IdCuentaBancaria);
    }
}
