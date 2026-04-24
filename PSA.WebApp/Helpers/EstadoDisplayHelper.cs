using System.Globalization;
using System.Text.RegularExpressions;

namespace PSA.WebApp.Helpers;

public static class EstadoDisplayHelper
{
    private static readonly Dictionary<string, string> EstadosConocidos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EnProceso"] = "En Proceso",
        ["En_Proceso"] = "En Proceso",
        ["EnRevision"] = "En Revisión",
        ["PendienteRevision"] = "Pendiente de Revisión",
        ["PendienteAprobacion"] = "Pendiente de Aprobación",
        ["PendienteAprobacionFinal"] = "Pendiente de Aprobación Final",
        ["PendienteDatosBancarios"] = "Pendiente de Datos Bancarios",
        ["NoCalifica"] = "No Califica",
        ["EvaluacionCompletada"] = "Evaluación Completada",
        ["PlanActivo"] = "Plan Activo",
        ["PlanVencido"] = "Plan Vencido",
        ["PlanCompletado"] = "Plan Completado",
        ["PagoPendiente"] = "Pago Pendiente",
        ["PagoRealizado"] = "Pago Realizado",
        ["NoLeida"] = "No Leída",
        ["Leida"] = "Leída",
        ["EnMora"] = "En Mora",
        ["BorradorGenerado"] = "Borrador Generado"
    };

    public static string FormatearEstado(this string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
        {
            return string.Empty;
        }

        var valor = estado.Trim();
        if (EstadosConocidos.TryGetValue(valor, out var conocido))
        {
            return conocido;
        }

        var normalizado = valor.Replace('_', ' ').Replace('-', ' ');
        normalizado = Regex.Replace(normalizado, "([a-záéíóúñ])([A-ZÁÉÍÓÚÑ])", "$1 $2", RegexOptions.CultureInvariant);
        normalizado = Regex.Replace(normalizado, "\\s+", " ", RegexOptions.CultureInvariant).Trim();

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return valor;
        }

        var enMayusculas = normalizado.Equals(normalizado.ToUpperInvariant(), StringComparison.Ordinal);
        normalizado = enMayusculas ? normalizado.ToLowerInvariant() : normalizado;

        var enTitulo = CultureInfo.GetCultureInfo("es-CR").TextInfo.ToTitleCase(normalizado.ToLowerInvariant());
        enTitulo = AjustarPalabrasFrecuentes(enTitulo);
        return enTitulo;
    }

    private static string AjustarPalabrasFrecuentes(string texto)
    {
        return texto
            .Replace(" De ", " de ", StringComparison.Ordinal)
            .Replace(" Del ", " del ", StringComparison.Ordinal)
            .Replace(" Y ", " y ", StringComparison.Ordinal)
            .Replace(" O ", " o ", StringComparison.Ordinal);
    }
}
