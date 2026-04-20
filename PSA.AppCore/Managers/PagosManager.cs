using PSA.AppCore.Services;
using PSA.AppCore.Services.Notifications;
using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Managers;

public class PagosManager(
    PlanPagoDAO planPagoDao,
    IPaymentPlanService paymentPlanService,
    IPaymentPlanReadService paymentPlanReadService,
    INotificationDispatcher notificationDispatcher)
{
    private static readonly HashSet<string> TiposCuentaPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ahorro",
        "Corriente",
        "IBAN",
        "SINPE",
        "Otra"
    };

    private readonly PlanPagoDAO _planPagoDao = planPagoDao;
    private readonly IPaymentPlanService _paymentPlanService = paymentPlanService;
    private readonly IPaymentPlanReadService _paymentPlanReadService = paymentPlanReadService;
    private readonly INotificationDispatcher _notificationDispatcher = notificationDispatcher;

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

        if (await _planPagoDao.ExistePlanPorFincaAnioAsync(request.IdFinca, request.Anio))
        {
            throw new InvalidOperationException("Ya existe un plan para la finca y año indicados. No se permite recalcular ni sobrescribir.");
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

    public Task<List<OwnerPaymentPlanDto>> ObtenerPlanesOwnerAsync(int idPropietario)
    {
        if (idPropietario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un propietario válido.");
        }

        return _paymentPlanReadService.ObtenerPlanesOwnerAsync(idPropietario);
    }

    public Task<OwnerPaymentPlanDetailDto?> ObtenerDetalleOwnerAsync(int idPropietario, int idPlanPago)
    {
        if (idPropietario <= 0 || idPlanPago <= 0)
        {
            throw new InvalidOperationException("Propietario o plan inválido.");
        }

        return _paymentPlanReadService.ObtenerDetalleOwnerAsync(idPropietario, idPlanPago);
    }

    public Task<List<PlanPagoResumenDTO>> ObtenerPlanesPendientesIngenieroAsync(int idIngeniero)
    {
        return _planPagoDao.ObtenerPlanesPendientesAprobacionIngenieroAsync(idIngeniero);
    }

    public Task<EngineerPaymentImpactDto?> ObtenerImpactoIngenieroAsync(int idIngeniero, int idEvaluacion)
    {
        if (idIngeniero <= 0 || idEvaluacion <= 0)
        {
            throw new InvalidOperationException("Ingeniero o evaluación inválidos.");
        }

        return _paymentPlanReadService.ObtenerImpactoIngenieroAsync(idIngeniero, idEvaluacion);
    }

    public Task<List<PlanPagoResumenDTO>> ObtenerPlanesConFiltrosAsync(FiltroPlanesPagoDTO filtro)
    {
        filtro ??= new FiltroPlanesPagoDTO();
        return _planPagoDao.ObtenerPlanesConFiltrosAsync(filtro);
    }

    public Task<List<AdminPaymentPlanDto>> ObtenerPlanesAdminAsync(AdminPaymentPlanFilterDto filtro)
    {
        filtro ??= new AdminPaymentPlanFilterDto();
        return _paymentPlanReadService.ObtenerPlanesAdminAsync(filtro);
    }

    public Task<AdminPaymentPlanDetailDto?> ObtenerDetalleAdminAsync(int idPlanPago)
    {
        if (idPlanPago <= 0)
        {
            throw new InvalidOperationException("Debe indicar un plan válido.");
        }

        return _paymentPlanReadService.ObtenerDetalleAdminAsync(idPlanPago);
    }

    public Task<List<CuentaBancariaDuenoDTO>> ObtenerCuentasBancariasDuenoAsync(int idUsuario)
    {
        if (idUsuario <= 0)
        {
            throw new InvalidOperationException("Debe indicar un usuario válido.");
        }

        return _planPagoDao.ObtenerCuentasBancariasDuenoAsync(idUsuario);
    }

    public async Task<int> RegistrarCuentaBancariaDuenoAsync(RegistrarCuentaBancariaDTO dto)
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

        var idCuenta = await _planPagoDao.RegistrarCuentaBancariaDuenoAsync(dto);
        if (idCuenta > 0)
        {
            await _notificationDispatcher.NotifyInAppAsync(
                dto.IdUsuario,
                "Cuenta bancaria registrada",
                $"La cuenta bancaria terminada en {ObtenerMascaraCuenta(dto.NumeroCuenta)} quedó registrada y pendiente de validación.",
                NotificationCatalog.TipoInfo,
                idCuenta);
        }

        return idCuenta;
    }

    private static string ObtenerMascaraCuenta(string numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
        {
            return "****";
        }

        var limpia = new string(numeroCuenta.Where(char.IsDigit).ToArray());
        if (limpia.Length <= 4)
        {
            return limpia;
        }

        return limpia[^4..];
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

    public async Task<int> ArrastrarSaldosPendientesAsync(int actorId, string? ip)
    {
        var afectados = await _planPagoDao.ArrastrarSaldosPendientesAsync(DateTime.UtcNow);
        return afectados;
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
