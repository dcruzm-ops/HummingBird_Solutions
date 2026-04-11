using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Reportes;

namespace PSA.DataAccess.DAO;

public class ReportesDAO
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReportesDAO(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ReportePagosDuenoDTO> ObtenerPagosDuenoAsync(int idPropietario, FiltroReporteDTO filtro)
    {
        const string sqlPagosVista = @"
SELECT IdPlanPago, IdFinca, NombreFinca, Anio, Mes, FechaProgramada,
       MontoBaseMensual, PorcentajeAjusteTotal, MontoMensualCalculado,
       MontoPendiente, EstadoCuota
FROM dbo.vw_ReportePagosMensualesDueno
WHERE IdPropietario = @IdPropietario
  AND (@Anio IS NULL OR Anio = @Anio)
  AND (@Mes IS NULL OR Mes = @Mes)
ORDER BY Anio DESC, Mes DESC, IdPlanPago DESC;";

        const string sqlPagosFallback = @"
SELECT
    pp.IdPlanPago, f.IdFinca, f.NombreFinca, pp.Anio, cp.Mes, cp.FechaProgramada,
    pp.MontoBaseMensual, pp.PorcentajeAjusteTotal, cp.MontoProgramado AS MontoMensualCalculado,
    cp.MontoPendiente, cp.EstadoCuota
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
INNER JOIN dbo.CuotasPago cp ON cp.IdPlanPago = pp.IdPlanPago
WHERE f.IdPropietario = @IdPropietario
  AND (@Anio IS NULL OR pp.Anio = @Anio)
  AND (@Mes IS NULL OR cp.Mes = @Mes)
ORDER BY pp.Anio DESC, cp.Mes DESC, pp.IdPlanPago DESC;";

        const string sqlAjustes = @"
SELECT DISTINCT
    pp.IdPlanPago,
    d.TipoFactor,
    d.ValorFactor,
    d.PorcentajeAjuste
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
INNER JOIN dbo.ConfiguracionPagoDetalle d ON d.IdConfiguracionPago = pp.IdConfiguracionPago
WHERE f.IdPropietario = @IdPropietario
  AND (@Anio IS NULL OR pp.Anio = @Anio)
  AND (
        (d.TipoFactor = 'Vegetacion' AND d.ValorFactor = f.Vegetacion)
     OR (d.TipoFactor = 'Pendiente' AND d.ValorFactor = f.Pendiente)
     OR (d.TipoFactor = 'UsoSuelo' AND d.ValorFactor = f.UsoSuelo)
     OR (d.TipoFactor = 'RecursosHidricos' AND d.ValorFactor = CASE WHEN f.TieneRecursosHidricos = 1 THEN 'Si' ELSE 'No' END)
  )
ORDER BY pp.IdPlanPago DESC, d.TipoFactor;";

        var resultado = new ReportePagosDuenoDTO();

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        var usarVistaPagos = await ExisteVistaAsync(connection, "vw_ReportePagosMensualesDueno");

        using (var command = new SqlCommand(usarVistaPagos ? sqlPagosVista : sqlPagosFallback, connection))
        {
            command.Parameters.AddWithValue("@IdPropietario", idPropietario);
            command.Parameters.AddWithValue("@Anio", (object?)filtro.Anio ?? DBNull.Value);
            command.Parameters.AddWithValue("@Mes", (object?)filtro.Mes ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.PagosMensuales.Add(new ItemPagoMensualDuenoDTO
                {
                    IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                    IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                    NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                    Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                    Mes = reader.GetInt32(reader.GetOrdinal("Mes")),
                    FechaProgramada = reader.GetDateTime(reader.GetOrdinal("FechaProgramada")),
                    MontoBaseMensual = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")),
                    PorcentajeAjusteTotal = reader.GetDecimal(reader.GetOrdinal("PorcentajeAjusteTotal")),
                    MontoMensualCalculado = reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
                    MontoPendiente = reader.GetDecimal(reader.GetOrdinal("MontoPendiente")),
                    EstadoCuota = reader["EstadoCuota"]?.ToString() ?? string.Empty
                });
            }
        }

        using (var command = new SqlCommand(sqlAjustes, connection))
        {
            command.Parameters.AddWithValue("@IdPropietario", idPropietario);
            command.Parameters.AddWithValue("@Anio", (object?)filtro.Anio ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                resultado.AjustesAplicados.Add(new ItemAjustePagoDTO
                {
                    IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                    TipoFactor = reader["TipoFactor"]?.ToString() ?? string.Empty,
                    ValorFactor = reader["ValorFactor"]?.ToString() ?? string.Empty,
                    PorcentajeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeAjuste"))
                });
            }
        }

        return resultado;
    }

    public async Task<List<ItemTransaccionDuenoDTO>> ObtenerTransaccionesDuenoAsync(int idPropietario, FiltroReporteDTO filtro)
    {
        const string sqlVista = @"
SELECT IdTransaccionPago, FechaTransaccion, MontoTotal, EstadoTransaccion,
       ReferenciaExterna, Observaciones, IdPlanPago, IdFinca, NombreFinca
FROM dbo.vw_ReporteTransaccionesPagosDueno
WHERE IdPropietario = @IdPropietario
  AND (@Anio IS NULL OR YEAR(FechaTransaccion) = @Anio)
  AND (@Mes IS NULL OR MONTH(FechaTransaccion) = @Mes)
ORDER BY FechaTransaccion DESC, IdTransaccionPago DESC;";

        const string sqlFallback = @"
SELECT
    tp.IdTransaccionPago, tp.FechaTransaccion, tp.MontoTotal, tp.EstadoTransaccion,
    tp.ReferenciaExterna, tp.Observaciones, pp.IdPlanPago, f.IdFinca, f.NombreFinca
FROM dbo.TransaccionesPago tp
INNER JOIN dbo.PlanesPago pp ON pp.IdPlanPago = tp.IdPlanPago
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
WHERE f.IdPropietario = @IdPropietario
  AND (@Anio IS NULL OR YEAR(tp.FechaTransaccion) = @Anio)
  AND (@Mes IS NULL OR MONTH(tp.FechaTransaccion) = @Mes)
ORDER BY tp.FechaTransaccion DESC, tp.IdTransaccionPago DESC;";

        var resultado = new List<ItemTransaccionDuenoDTO>();
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        var usarVista = await ExisteVistaAsync(connection, "vw_ReporteTransaccionesPagosDueno");
        using var command = new SqlCommand(usarVista ? sqlVista : sqlFallback, connection);
        command.Parameters.AddWithValue("@IdPropietario", idPropietario);
        command.Parameters.AddWithValue("@Anio", (object?)filtro.Anio ?? DBNull.Value);
        command.Parameters.AddWithValue("@Mes", (object?)filtro.Mes ?? DBNull.Value);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultado.Add(new ItemTransaccionDuenoDTO
            {
                IdTransaccionPago = reader.GetInt32(reader.GetOrdinal("IdTransaccionPago")),
                FechaTransaccion = reader.GetDateTime(reader.GetOrdinal("FechaTransaccion")),
                MontoTotal = reader.GetDecimal(reader.GetOrdinal("MontoTotal")),
                EstadoTransaccion = reader["EstadoTransaccion"]?.ToString() ?? string.Empty,
                ReferenciaExterna = reader["ReferenciaExterna"] as string,
                Observaciones = reader["Observaciones"] as string,
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty
            });
        }

        return resultado;
    }

    public async Task<ReporteEvaluacionesIngenieroDTO> ObtenerEvaluacionesIngenieroAsync(int idIngeniero, FiltroReporteDTO filtro)
    {
        const string sqlVista = @"
SELECT IdEvaluacion, IdIngeniero, Ingeniero, EstadoEvaluacion, DecisionTecnica,
       FechaVisita, FechaDecision, IdFinca, NombreFinca, Provincia, Canton, Distrito
FROM dbo.vw_ReporteEvaluacionesIngeniero
WHERE IdIngeniero = @IdIngeniero
  AND (@Anio IS NULL OR YEAR(COALESCE(FechaDecision, FechaVisita)) = @Anio)
  AND (@Mes IS NULL OR MONTH(COALESCE(FechaDecision, FechaVisita)) = @Mes)
ORDER BY COALESCE(FechaDecision, FechaVisita) DESC, IdEvaluacion DESC;";

        const string sqlFallback = @"
SELECT
    e.IdEvaluacion, e.IdIngeniero, ISNULL(u.NombreCompleto, '') AS Ingeniero, e.EstadoEvaluacion, e.DecisionTecnica,
    e.FechaVisita, e.FechaDecision, f.IdFinca, f.NombreFinca, f.Provincia, f.Canton, f.Distrito
FROM dbo.EvaluacionesTecnicas e
LEFT JOIN dbo.Usuarios u ON u.IdUsuario = e.IdIngeniero
INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdIngeniero = @IdIngeniero
  AND (@Anio IS NULL OR YEAR(COALESCE(e.FechaDecision, e.FechaVisita)) = @Anio)
  AND (@Mes IS NULL OR MONTH(COALESCE(e.FechaDecision, e.FechaVisita)) = @Mes)
ORDER BY COALESCE(e.FechaDecision, e.FechaVisita) DESC, e.IdEvaluacion DESC;";

        var resultado = new ReporteEvaluacionesIngenieroDTO();

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        var usarVista = await ExisteVistaAsync(connection, "vw_ReporteEvaluacionesIngeniero");
        using var command = new SqlCommand(usarVista ? sqlVista : sqlFallback, connection);
        command.Parameters.AddWithValue("@IdIngeniero", idIngeniero);
        command.Parameters.AddWithValue("@Anio", (object?)filtro.Anio ?? DBNull.Value);
        command.Parameters.AddWithValue("@Mes", (object?)filtro.Mes ?? DBNull.Value);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var item = new ItemEvaluacionIngenieroDTO
            {
                IdEvaluacion = reader.GetInt32(reader.GetOrdinal("IdEvaluacion")),
                IdIngeniero = reader["IdIngeniero"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdIngeniero")),
                Ingeniero = reader["Ingeniero"]?.ToString() ?? string.Empty,
                EstadoEvaluacion = reader["EstadoEvaluacion"]?.ToString() ?? string.Empty,
                DecisionTecnica = reader["DecisionTecnica"] as string,
                FechaVisita = reader["FechaVisita"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaVisita")),
                FechaDecision = reader["FechaDecision"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaDecision")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                Canton = reader["Canton"]?.ToString() ?? string.Empty,
                Distrito = reader["Distrito"]?.ToString() ?? string.Empty
            };

            resultado.Evaluaciones.Add(item);
        }

        resultado.Total = resultado.Evaluaciones.Count;
        resultado.TotalCalifica = resultado.Evaluaciones.Count(x => string.Equals(x.DecisionTecnica, "Califica", StringComparison.OrdinalIgnoreCase));
        resultado.TotalNoCalifica = resultado.Evaluaciones.Count(x => string.Equals(x.DecisionTecnica, "No Califica", StringComparison.OrdinalIgnoreCase));

        return resultado;
    }

    public async Task<List<ItemPagoUbicacionDTO>> ObtenerPagosPorUbicacionAsync(FiltroReporteDTO filtro)
    {
        const string sqlVista = @"
SELECT Provincia, Canton, Distrito, Anio, Mes, MontoPagadoMes, MontoProgramadoMes
FROM dbo.vw_ReportePagosPorUbicacion
WHERE (@Anio IS NULL OR Anio = @Anio)
  AND (@Mes IS NULL OR Mes = @Mes)
ORDER BY Anio DESC, Mes DESC, Provincia, Canton, Distrito;";

        const string sqlFallback = @"
SELECT
    f.Provincia,
    f.Canton,
    f.Distrito,
    pp.Anio,
    cp.Mes,
    SUM(cp.MontoProgramado - cp.MontoPendiente) AS MontoPagadoMes,
    SUM(cp.MontoProgramado) AS MontoProgramadoMes
FROM dbo.CuotasPago cp
INNER JOIN dbo.PlanesPago pp ON pp.IdPlanPago = cp.IdPlanPago
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
WHERE (@Anio IS NULL OR pp.Anio = @Anio)
  AND (@Mes IS NULL OR cp.Mes = @Mes)
GROUP BY f.Provincia, f.Canton, f.Distrito, pp.Anio, cp.Mes
ORDER BY pp.Anio DESC, cp.Mes DESC, f.Provincia, f.Canton, f.Distrito;";

        var resultado = new List<ItemPagoUbicacionDTO>();
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        var usarVista = await ExisteVistaAsync(connection, "vw_ReportePagosPorUbicacion");
        using var command = new SqlCommand(usarVista ? sqlVista : sqlFallback, connection);
        command.Parameters.AddWithValue("@Anio", (object?)filtro.Anio ?? DBNull.Value);
        command.Parameters.AddWithValue("@Mes", (object?)filtro.Mes ?? DBNull.Value);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultado.Add(new ItemPagoUbicacionDTO
            {
                Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                Canton = reader["Canton"]?.ToString() ?? string.Empty,
                Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                Mes = reader.GetInt32(reader.GetOrdinal("Mes")),
                MontoPagadoMes = reader.GetDecimal(reader.GetOrdinal("MontoPagadoMes")),
                MontoProgramadoMes = reader.GetDecimal(reader.GetOrdinal("MontoProgramadoMes"))
            });
        }

        return resultado;
    }

    public async Task<List<ItemResumenActividadDTO>> ObtenerResumenActividadAsync()
    {
        const string sqlVista = @"
SELECT Indicador, Total
FROM dbo.vw_ResumenActividadSistema
ORDER BY Indicador;";

        const string sqlFallback = @"
SELECT 'Usuarios activos' AS Indicador, CAST(COUNT_BIG(1) AS BIGINT) AS Total
FROM dbo.Usuarios
WHERE Estado = 'Activo'
UNION ALL
SELECT 'Fincas registradas', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.Fincas
UNION ALL
SELECT 'Evaluaciones pendientes', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.EvaluacionesTecnicas
WHERE EstadoEvaluacion IN ('Pendiente', 'En proceso', 'En Proceso')
UNION ALL
SELECT 'Planes de pago activos', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.PlanesPago
WHERE EstadoPlan = 'Activo'
UNION ALL
SELECT 'Transacciones procesadas', CAST(COUNT_BIG(1) AS BIGINT)
FROM dbo.TransaccionesPago
WHERE EstadoTransaccion = 'Procesada';";

        var resultado = new List<ItemResumenActividadDTO>();
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        var usarVista = await ExisteVistaAsync(connection, "vw_ResumenActividadSistema");
        using var command = new SqlCommand(usarVista ? sqlVista : sqlFallback, connection);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            resultado.Add(new ItemResumenActividadDTO
            {
                Indicador = reader["Indicador"]?.ToString() ?? string.Empty,
                Total = reader.GetInt64(reader.GetOrdinal("Total"))
            });
        }

        return resultado;
    }

    private static async Task<bool> ExisteVistaAsync(SqlConnection connection, string nombreVista)
    {
        const string sql = @"
SELECT CASE WHEN OBJECT_ID(@NombreVista, 'V') IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END;";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@NombreVista", $"dbo.{nombreVista}");
        var result = await command.ExecuteScalarAsync();
        return result is bool value && value;
    }
}
