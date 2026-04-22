using PSA.AppCore.Services;
using PSA.EntidadesDTO.DTOs.Pagos;
using Xunit;

namespace PSA.AppCore.Tests;

public class PaymentCalculationServiceTests
{
    private readonly IPaymentCalculationService _service = new PaymentCalculationService();

    [Fact]
    public void Calculate_AppliesConfiguredCap()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 10.5m,
            VegetacionFinal = "bosque primario",
            TieneRecursosHidricosFinal = true,
            TieneRiosOQuebradasFinal = true,
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
        Assert.Equal(315m, result.MontoAjusteVegetacion);
        Assert.Equal(105m, result.MontoAjusteRiosQuebradas);
        Assert.Equal(105m, result.MontoAjusteNacientes);
        Assert.Equal(210m, result.MontoAjustePendiente);
    }

    [Fact]
    public void Calculate_SupportsDecimalHectares_WithoutHydric()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 3.75m,
            VegetacionFinal = "plantación / arbustos",
            TieneRecursosHidricosFinal = false,
            TieneRiosOQuebradasFinal = false,
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

    [Fact]
    public void Calculate_Throws_WhenApprovedHectaresAreNegative()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = -0.01m,
            VegetacionFinal = "bosque",
            TieneRecursosHidricosFinal = false,
            TieneRiosOQuebradasFinal = false,
            CantidadNacientesFinal = 0,
            PendienteFinal = "plana"
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 100m,
            TopePorcentajeAjuste = 40m
        };

        var ex = Assert.Throws<InvalidOperationException>(() => _service.Calculate(context, config));

        Assert.Equal("Las hectáreas aprobadas no pueden ser negativas.", ex.Message);
    }

    [Fact]
    public void Calculate_TrimsInputAndHandlesMissingPercentages()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 1m,
            VegetacionFinal = "  bosque secundario  ",
            TieneRecursosHidricosFinal = true,
            TieneRiosOQuebradasFinal = true,
            CantidadNacientesFinal = 1,
            PendienteFinal = "desconocida"
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 100m,
            TopePorcentajeAjuste = 99m,
            VegetacionAjustes = new(StringComparer.OrdinalIgnoreCase) { ["bosque secundario"] = 15m },
            HidricosAjustes = new(StringComparer.OrdinalIgnoreCase) { ["Si"] = 8m }
        };

        var result = _service.Calculate(context, config);

        Assert.Equal(100m, result.MontoBaseMensual);
        Assert.Equal(15m, result.PorcentajeVegetacion);
        Assert.Equal(8m, result.PorcentajeHidrico);
        Assert.Equal(0m, result.PorcentajeNacientes);
        Assert.Equal(0m, result.PorcentajePendiente);
        Assert.Equal(23m, result.PorcentajeAjusteAplicado);
        Assert.Equal(123m, result.MontoMensualTotal);
    }

    [Fact]
    public void Calculate_AcceptsLegacyHydricAliasFromConfiguration()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 2m,
            TieneRiosOQuebradasFinal = true
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 100m,
            TopePorcentajeAjuste = 40m,
            HidricosAjustes = new(StringComparer.OrdinalIgnoreCase) { ["Con recursos"] = 10m }
        };

        var result = _service.Calculate(context, config);

        Assert.Equal(10m, result.PorcentajeRiosQuebradas);
        Assert.Equal(20m, result.MontoAjusteRiosQuebradas);
    }

    [Fact]
    public void Calculate_AcceptsHydricLabelWithSlashAndAccent()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 4m,
            TieneRiosOQuebradasFinal = true
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 100m,
            TopePorcentajeAjuste = 40m,
            HidricosAjustes = new(StringComparer.OrdinalIgnoreCase) { ["Ríos/quebradas"] = 10m }
        };

        var result = _service.Calculate(context, config);

        Assert.Equal(10m, result.PorcentajeRiosQuebradas);
        Assert.Equal(40m, result.MontoAjusteRiosQuebradas);
    }

    [Fact]
    public void Calculate_UsesConfiguredCapWithoutHardcode()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 12m,
            VegetacionFinal = "bosque",
            TieneRecursosHidricosFinal = true,
            TieneRiosOQuebradasFinal = true,
            CantidadNacientesFinal = 2,
            PendienteFinal = "inclinada"
        };

        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 200m,
            TopePorcentajeAjuste = 65m,
            VegetacionAjustes = new(StringComparer.OrdinalIgnoreCase) { ["bosque"] = 30m },
            HidricosAjustes = new(StringComparer.OrdinalIgnoreCase) { ["Si"] = 20m, ["Naciente"] = 10m },
            PendienteAjustes = new(StringComparer.OrdinalIgnoreCase) { ["inclinada"] = 20m }
        };

        var result = _service.Calculate(context, config);

        Assert.Equal(70m, result.PorcentajeAjusteTotalBruto);
        Assert.Equal(65m, result.PorcentajeAjusteAplicado);
        Assert.Equal(65m, result.TopePorcentajeAjuste);
    }

    [Fact]
    public void Calculate_Throws_WhenNacientesAreNegative()
    {
        var context = new PlanPagoGenerationContextDTO
        {
            HectareasAprobadas = 1m,
            CantidadNacientesFinal = -1
        };
        var config = new PaymentConfigurationVersionDTO
        {
            PrecioBasePorHectarea = 100m,
            TopePorcentajeAjuste = 40m
        };

        var ex = Assert.Throws<InvalidOperationException>(() => _service.Calculate(context, config));
        Assert.Equal("La cantidad de nacientes no puede ser negativa.", ex.Message);
    }
}
