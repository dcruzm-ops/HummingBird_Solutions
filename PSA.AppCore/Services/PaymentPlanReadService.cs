using PSA.DataAccess.DAO;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Services;

public interface IPaymentPlanReadService
{
    Task<List<OwnerPaymentPlanDto>> ObtenerPlanesOwnerAsync(int idPropietario);
    Task<OwnerPaymentPlanDetailDto?> ObtenerDetalleOwnerAsync(int idPropietario, int idPlanPago);
    Task<EngineerPaymentImpactDto?> ObtenerImpactoIngenieroAsync(int idIngeniero, int idEvaluacion);
    Task<List<AdminPaymentPlanDto>> ObtenerPlanesAdminAsync(AdminPaymentPlanFilterDto filtro);
    Task<AdminPaymentPlanDetailDto?> ObtenerDetalleAdminAsync(int idPlanPago);
}

public class PaymentPlanReadService(PlanPagoDAO planPagoDao) : IPaymentPlanReadService
{
    private readonly PlanPagoDAO _planPagoDao = planPagoDao;

    public Task<List<OwnerPaymentPlanDto>> ObtenerPlanesOwnerAsync(int idPropietario)
        => _planPagoDao.ObtenerPlanesOwnerAsync(idPropietario);

    public Task<OwnerPaymentPlanDetailDto?> ObtenerDetalleOwnerAsync(int idPropietario, int idPlanPago)
        => _planPagoDao.ObtenerDetalleOwnerAsync(idPropietario, idPlanPago);

    public Task<EngineerPaymentImpactDto?> ObtenerImpactoIngenieroAsync(int idIngeniero, int idEvaluacion)
        => _planPagoDao.ObtenerImpactoPagoIngenieroAsync(idIngeniero, idEvaluacion);

    public Task<List<AdminPaymentPlanDto>> ObtenerPlanesAdminAsync(AdminPaymentPlanFilterDto filtro)
        => _planPagoDao.ObtenerPlanesAdminAsync(filtro);

    public Task<AdminPaymentPlanDetailDto?> ObtenerDetalleAdminAsync(int idPlanPago)
        => _planPagoDao.ObtenerDetalleAdminAsync(idPlanPago);
}
