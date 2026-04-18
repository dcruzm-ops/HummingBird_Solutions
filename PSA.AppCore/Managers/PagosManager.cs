using PSA.AppCore.Services;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Managers;

public class PagosManager(
    PlanPagoDAO planPagoDao,
    IPaymentPlanService paymentPlanService)
{
    private readonly PlanPagoDAO _planPagoDao = planPagoDao;
    private readonly IPaymentPlanService _paymentPlanService = paymentPlanService;

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

        if (!request.Simular)
        {
            return await _planPagoDao.GenerarPlanPagoAsync(request);
        }

        return await _planPagoDao.GenerarPlanPagoAsync(request);
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

    public Task<bool> AsociarCuentaPlanAsync(int idPlanPago, AsociarCuentaPlanDTO dto, string? ip)
    {
        if (idPlanPago <= 0)
        {
            throw new InvalidOperationException("Debe indicar un plan válido.");
        }

        if (dto.IdUsuario <= 0 || dto.IdCuentaBancaria <= 0)
        {
            throw new InvalidOperationException("Debe indicar usuario y cuenta bancaria válidos.");
        }

        return _paymentPlanService.AttachBankAccountAsync(idPlanPago, dto.IdUsuario, dto.IdCuentaBancaria, ip);
    }

    public Task<bool> AprobarPlanFinalAsync(int idPlanPago, AprobarPlanPagoFinalDTO dto, string? ip)
    {
        if (idPlanPago <= 0 || dto.IdIngeniero <= 0)
        {
            throw new InvalidOperationException("Plan o ingeniero inválido.");
        }

        return _paymentPlanService.ApproveFinalActivationAsync(idPlanPago, dto.IdIngeniero, ip);
    }
}
