using PSA.AppCore.Services;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.AppCore.Tests;

public class PaymentCalculationServiceTests
{
    private readonly IPaymentCalculationService _service = new PaymentCalculationService();

    [Fact]
    public void Calculate_AppliesCapAtFortyPercent()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 10.5m,
            VegetacionFinal = "bosque primario",
            TieneRecursosHidricosFinal = true,
            CantidadNacientesFinal = 2,
            PendienteFinal = "muy inclinada"
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 100m,
            TopePorcentajeAjuste = 40m,
            VegetacionAjustes = new(StringComparer.OrdinalIgnoreCase) { ["bosque primario"] = 30m },
            HidricosAjustes = new(StringComparer.OrdinalIgnoreCase) { ["Si"] = 10m, ["Naciente"] = 5m },
            PendienteAjustes = new(StringComparer.OrdinalIgnoreCase) { ["muy inclinada"] = 20m }
        };

        var result = _service.Calculate(context, config);

        Assert.Equal(1050m, result.MontoBaseMensual);
        Assert.Equal(70m, result.PorcentajeAjusteTotalBruto);
        Assert.Equal(40m, result.PorcentajeAjusteAplicado);
        Assert.Equal(420m, result.MontoAjusteMensual);
        Assert.Equal(1470m, result.MontoMensualTotal);
    }

    [Fact]
    public void Calculate_SupportsDecimalHectares_WithoutHydric()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 3.75m,
            VegetacionFinal = "plantación / arbustos",
            TieneRecursosHidricosFinal = false,
            CantidadNacientesFinal = 0,
            PendienteFinal = "inclinada"
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 123.45m,
            TopePorcentajeAjuste = 40m,
            VegetacionAjustes = new(StringComparer.OrdinalIgnoreCase) { ["plantación / arbustos"] = 10m },
            PendienteAjustes = new(StringComparer.OrdinalIgnoreCase) { ["inclinada"] = 10m }
        };

        var result = _service.Calculate(context, config);

        Assert.Equal(462.94m, result.MontoBaseMensual);
        Assert.Equal(20m, result.PorcentajeAjusteAplicado);
        Assert.Equal(92.59m, result.MontoAjusteMensual);
        Assert.Equal(555.53m, result.MontoMensualTotal);
        Assert.Equal(6666.36m, result.MontoAnualTotal);
    }
}
