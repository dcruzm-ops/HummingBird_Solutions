namespace PSA.AppCore.Services.Notifications;

public static class NotificationCatalog
{
    public const string TipoInfo = "info";
    public const string TipoSuccess = "success";
    public const string TipoWarning = "warning";

    public static string EmailResultadoEvaluacion(string nombreUsuario, string nombreFinca, string decision, string? observaciones)
    {
        var observacionesTexto = string.IsNullOrWhiteSpace(observaciones)
            ? "Sin observaciones adicionales."
            : observaciones.Trim();

        return $"""
<h2>Resultado de evaluación técnica</h2>
<p>Hola {nombreUsuario},</p>
<p>La evaluación técnica de la finca <strong>{nombreFinca}</strong> finalizó con el estado: <strong>{decision}</strong>.</p>
<p><strong>Observaciones:</strong> {observacionesTexto}</p>
<p>Puede ingresar al sistema PSA Costa Rica para revisar el detalle completo.</p>
""";
    }

    public static string EmailPlanPago(string nombreUsuario, string nombreFinca, int idPlanPago)
        => $"""
<h2>Plan de pagos generado</h2>
<p>Hola {nombreUsuario},</p>
<p>Se generó el plan de pago #{idPlanPago} para la finca <strong>{nombreFinca}</strong>.</p>
<p>Revise el detalle de cuotas desde su panel de pagos.</p>
""";

    public static string EmailCuentaBancaria(string nombreUsuario, bool aprobada, string? observaciones)
    {
        var estado = aprobada ? "validada" : "rechazada";
        var observacionesTexto = string.IsNullOrWhiteSpace(observaciones)
            ? "Sin observaciones adicionales."
            : observaciones.Trim();

        return $"""
<h2>Resultado de validación de cuenta bancaria</h2>
<p>Hola {nombreUsuario},</p>
<p>Su cuenta bancaria fue <strong>{estado}</strong>.</p>
<p><strong>Observaciones:</strong> {observacionesTexto}</p>
<p>Si corresponde, puede registrar una nueva cuenta en el módulo de pagos.</p>
""";
    }
}
