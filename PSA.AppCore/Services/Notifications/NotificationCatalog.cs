namespace PSA.AppCore.Services.Notifications;

public static class NotificationCatalog
{
    public const string TipoInfo = "info";
    public const string TipoSuccess = "success";
    public const string TipoWarning = "warning";

    public static string EmailResultadoEvaluacion(string nombreUsuario, string nombreFinca, string decision, DateTime fecha, string? observaciones, string? resumenCambios, string? enlaceSistema)
    {
        var observacionesTexto = string.IsNullOrWhiteSpace(observaciones) ? "Sin observaciones adicionales." : observaciones.Trim();
        var resumenTexto = string.IsNullOrWhiteSpace(resumenCambios) ? "No se registraron ajustes adicionales." : resumenCambios.Trim();
        var estado = decision.Equals("Califica", StringComparison.OrdinalIgnoreCase) ? "CALIFICA" : "NO CALIFICA";

        return ConstruirPlantillaMarca(
            "Resultado de evaluación técnica",
            $"""
<p>Hola {nombreUsuario},</p>
<p>La evaluación técnica de la finca <strong>{nombreFinca}</strong> fue finalizada.</p>
<p><strong>Resultado:</strong> {estado}</p>
<p><strong>Fecha de resolución:</strong> {fecha:dd/MM/yyyy}</p>
<p><strong>Observaciones:</strong> {observacionesTexto}</p>
<p><strong>Resumen de ajustes del ingeniero:</strong> {resumenTexto}</p>
{ConstruirBloqueEnlace(enlaceSistema)}
<p>PSA Costa Rica</p>
""");
    }

    public static string EmailPlanPago(string nombreUsuario, string nombreFinca, int idPlanPago, string periodoPlan, decimal montoEstimado, string? enlaceSistema)
        => ConstruirPlantillaMarca(
            "Plan de pagos generado",
            $"""
<p>Hola {nombreUsuario},</p>
<p>Se generó correctamente el plan de pagos asociado a la finca <strong>{nombreFinca}</strong>.</p>
<p><strong>Id plan:</strong> #{idPlanPago}</p>
<p><strong>Período del plan:</strong> {periodoPlan}</p>
<p><strong>Monto estimado:</strong> {montoEstimado:N2}</p>
{ConstruirBloqueEnlace(enlaceSistema)}
<p>PSA Costa Rica</p>
""");

    public static string EmailCuentaBancaria(
        string nombreUsuario,
        bool aprobada,
        string banco,
        string cuentaMascara,
        DateTime fecha,
        string? motivo,
        string? enlaceSistema)
    {
        var estado = aprobada ? "validada correctamente" : "rechazada";
        var motivoTexto = string.IsNullOrWhiteSpace(motivo) ? "Sin observaciones adicionales." : motivo.Trim();

        return ConstruirPlantillaMarca(
            aprobada ? "Cuenta bancaria aprobada" : "Cuenta bancaria rechazada",
            $"""
<p>Hola {nombreUsuario},</p>
<p>La cuenta bancaria registrada para el proceso PSA fue <strong>{estado}</strong>.</p>
<p><strong>Banco:</strong> {banco}</p>
<p><strong>Cuenta:</strong> {cuentaMascara}</p>
<p><strong>Fecha de revisión:</strong> {fecha:dd/MM/yyyy HH:mm}</p>
<p><strong>Motivo/Observaciones:</strong> {motivoTexto}</p>
{ConstruirBloqueEnlace(enlaceSistema)}
<p>PSA Costa Rica</p>
""");
    }

    private static string ConstruirPlantillaMarca(string titulo, string cuerpo)
        => $"""
<html>
<body style='font-family:Arial,Helvetica,sans-serif;background:#f3f4f6;padding:24px;color:#1f2937;'>
  <div style='max-width:680px;margin:0 auto;background:#ffffff;border:1px solid #d9e2ec;border-radius:12px;overflow:hidden;'>
    <div style='background:#0f172a;color:#ffffff;padding:16px 22px;font-size:20px;font-weight:700;'>PSA Costa Rica</div>
    <div style='padding:22px;'>
      <h2 style='margin-top:0;color:#111827;'>{titulo}</h2>
      {cuerpo}
    </div>
  </div>
</body>
</html>
""";

    private static string ConstruirBloqueEnlace(string? enlaceSistema)
    {
        if (string.IsNullOrWhiteSpace(enlaceSistema))
        {
            return "<p>Ingresa al sistema PSA Costa Rica para revisar el detalle completo.</p>";
        }

        return $"<p><a href='{enlaceSistema}' style='display:inline-block;background:#16a34a;color:#fff;padding:10px 15px;border-radius:6px;text-decoration:none;'>Ir al sistema</a></p>";
    }
}
