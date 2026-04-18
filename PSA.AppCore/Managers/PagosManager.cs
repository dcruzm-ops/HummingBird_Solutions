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
}
