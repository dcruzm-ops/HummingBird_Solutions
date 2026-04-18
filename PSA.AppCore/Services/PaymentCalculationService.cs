using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Services;

public interface IPaymentCalculationService
{
    PaymentCalculationResultDTO Calculate(PlanPagoGenerationContextDTO context, PaymentConfigurationVersionDTO config);
}

public class PaymentCalculationService : IPaymentCalculationService
{
    public PaymentCalculationResultDTO Calculate(PlanPagoGenerationContextDTO context, PaymentConfigurationVersionDTO config)
    {
        if (context.HectareasAprobadas < 0)
        {
            throw new InvalidOperationException("Las hectáreas aprobadas no pueden ser negativas.");
        }

        var porcentajeVegetacion = ResolvePercentage(config.VegetacionAjustes, context.VegetacionFinal);
        var porcentajeHidrico = context.TieneRecursosHidricosFinal
            ? ResolvePercentage(config.HidricosAjustes, "Si")
            : 0m;
        var porcentajePorNaciente = ResolvePercentage(config.HidricosAjustes, "Naciente");
        var porcentajeNacientes = context.CantidadNacientesFinal > 0
            ? porcentajePorNaciente * context.CantidadNacientesFinal
            : 0m;
        var porcentajePendiente = ResolvePercentage(config.PendienteAjustes, context.PendienteFinal);

        var montoBaseMensual = Round2(context.HectareasAprobadas * config.PrecioBasePorHectarea);
        var porcentajeBruto = porcentajeVegetacion + porcentajeHidrico + porcentajeNacientes + porcentajePendiente;
        var porcentajeAplicado = Math.Min(porcentajeBruto, config.TopePorcentajeAjuste);
        var montoAjusteMensual = Round2(montoBaseMensual * (porcentajeAplicado / 100m));
        var montoMensualTotal = Round2(montoBaseMensual + montoAjusteMensual);

        return new PaymentCalculationResultDTO
        {
            MontoBaseMensual = montoBaseMensual,
            MontoAjusteMensual = montoAjusteMensual,
            MontoMensualTotal = montoMensualTotal,
            MontoAnualTotal = Round2(montoMensualTotal * 12m),
            PorcentajeVegetacion = porcentajeVegetacion,
            PorcentajeHidrico = porcentajeHidrico,
            PorcentajeNacientes = porcentajeNacientes,
            PorcentajePendiente = porcentajePendiente,
            PorcentajeAjusteTotalBruto = porcentajeBruto,
            PorcentajeAjusteAplicado = porcentajeAplicado,
            TopePorcentajeAjuste = config.TopePorcentajeAjuste
        };
    }

    private static decimal ResolvePercentage(IReadOnlyDictionary<string, decimal> source, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        return source.TryGetValue(value.Trim(), out var percentage) ? percentage : 0m;
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
