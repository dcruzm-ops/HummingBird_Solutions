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

        if (context.CantidadNacientesFinal < 0)
        {
            throw new InvalidOperationException("La cantidad de nacientes no puede ser negativa.");
        }

        var porcentajeVegetacion = ResolvePercentage(config.VegetacionAjustes, context.VegetacionFinal);
        var porcentajeRiosQuebradas = context.TieneRiosOQuebradasFinal
            ? ResolvePercentage(config.HidricosAjustes, "RiosQuebradas", "Si")
            : 0m;
        var porcentajePorNaciente = ResolvePercentage(config.HidricosAjustes, "Naciente", "Nacientes");
        var porcentajeNacientes = context.CantidadNacientesFinal > 0
            ? porcentajePorNaciente * context.CantidadNacientesFinal
            : 0m;
        var porcentajePendiente = ResolvePercentage(config.PendienteAjustes, context.PendienteFinal);

        var montoBaseMensual = Round2(context.HectareasAprobadas * config.PrecioBasePorHectarea);
        var montoAjusteVegetacion = Round2(montoBaseMensual * (porcentajeVegetacion / 100m));
        var montoAjusteRiosQuebradas = Round2(montoBaseMensual * (porcentajeRiosQuebradas / 100m));
        var montoAjusteNacientes = Round2(montoBaseMensual * (porcentajeNacientes / 100m));
        var montoAjustePendiente = Round2(montoBaseMensual * (porcentajePendiente / 100m));

        var porcentajeBruto = porcentajeVegetacion + porcentajeRiosQuebradas + porcentajeNacientes + porcentajePendiente;
        var topeOperativo = Math.Max(config.TopePorcentajeAjuste, 0m);
        var porcentajeAplicado = Math.Min(porcentajeBruto, topeOperativo);
        var porcentajeRecortado = Math.Max(porcentajeBruto - porcentajeAplicado, 0m);
        var montoAjusteBruto = Round2(montoBaseMensual * (porcentajeBruto / 100m));
        var montoAjusteMensual = Round2(montoBaseMensual * (porcentajeAplicado / 100m));
        var montoRecortado = Round2(montoAjusteBruto - montoAjusteMensual);
        var montoMensualTotal = Round2(montoBaseMensual + montoAjusteMensual);

        return new PaymentCalculationResultDTO
        {
            MontoBaseMensual = montoBaseMensual,
            MontoAjusteMensual = montoAjusteMensual,
            MontoMensualTotal = montoMensualTotal,
            MontoAnualTotal = Round2(montoMensualTotal * 12m),
            PorcentajeVegetacion = porcentajeVegetacion,
            MontoAjusteVegetacion = montoAjusteVegetacion,
            PorcentajeRiosQuebradas = porcentajeRiosQuebradas,
            MontoAjusteRiosQuebradas = montoAjusteRiosQuebradas,
            PorcentajeHidrico = porcentajeRiosQuebradas,
            PorcentajeNacientes = porcentajeNacientes,
            MontoAjusteNacientes = montoAjusteNacientes,
            PorcentajePendiente = porcentajePendiente,
            MontoAjustePendiente = montoAjustePendiente,
            PorcentajeAjusteTotalBruto = porcentajeBruto,
            PorcentajeAjusteAplicado = porcentajeAplicado,
            PorcentajeRecortadoPorTope = porcentajeRecortado,
            TopePorcentajeAjuste = topeOperativo,
            MontoAjusteBrutoMensual = montoAjusteBruto,
            MontoRecortadoPorTope = montoRecortado
        };
    }

    private static decimal ResolvePercentage(IReadOnlyDictionary<string, decimal> source, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (source.TryGetValue(key.Trim(), out var percentage))
            {
                return percentage;
            }
        }

        return 0m;
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
