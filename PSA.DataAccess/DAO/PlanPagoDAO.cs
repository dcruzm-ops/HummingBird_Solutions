using System.Data;
using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.DataAccess.DAO;

public class PlanPagoDAO(IDbConnectionFactory connectionFactory)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;


    public async Task<bool> ExistePlanPorFincaAnioAsync(int idFinca, int anio)
    {
        const string sql = @"SELECT TOP 1 1 FROM dbo.PlanesPago WHERE IdFinca=@IdFinca AND Anio=@Anio;";
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdFinca", idFinca);
        command.Parameters.AddWithValue("@Anio", anio);
        await connection.OpenAsync();
        return (await command.ExecuteScalarAsync()) != null;
    }

    public async Task<PlanPagoDTO?> GenerarPlanPagoAsync(GenerarPlanPagoRequestDTO request)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.SP_Pagos_GenerarPlanPago", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@IdFinca", request.IdFinca);
        command.Parameters.AddWithValue("@Anio", request.Anio);
        command.Parameters.AddWithValue("@Simular", request.Simular);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapPlanPago(reader);
    }

    public async Task<PlanPagoGenerationContextDTO?> ObtenerContextoGeneracionDesdeEvaluacionAsync(int idEvaluacion)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var existeColumnaRiosAjustado = await ExisteColumnaAsync(connection, "EvaluacionesTecnicas", "TieneRiosOQuebradasAjustado");
        var existeColumnaNacientesAjustada = await ExisteColumnaAsync(connection, "EvaluacionesTecnicas", "CantidadNacientesAjustada");

        var exprRiosFinal = existeColumnaRiosAjustado
            ? "COALESCE(e.TieneRiosOQuebradasAjustado, f.TieneRiosOQuebradas)"
            : "f.TieneRiosOQuebradas";
        var exprNacientesFinal = existeColumnaNacientesAjustada
            ? "COALESCE(e.CantidadNacientesAjustada, f.CantidadNacientes, 0)"
            : "COALESCE(f.CantidadNacientes, 0)";

        var sql = $@"
SELECT TOP 1
    e.IdEvaluacion,
    f.IdFinca,
    f.IdPropietario,
    f.NombreFinca,
    f.Hectareas AS HectareasOriginales,
    COALESCE(e.HectareasAjustadas, f.Hectareas) AS HectareasAprobadas,
    f.Vegetacion AS VegetacionOriginal,
    COALESCE(NULLIF(e.VegetacionAjustada, ''), f.Vegetacion) AS VegetacionFinal,
    f.TieneRiosOQuebradas AS TieneRiosOQuebradasOriginal,
    {exprRiosFinal} AS TieneRiosOQuebradasFinal,
    CAST(CASE WHEN {exprRiosFinal} = 1 OR {exprNacientesFinal} > 0 THEN 1 ELSE 0 END AS bit) AS TieneRecursosHidricosFinal,
    COALESCE(f.CantidadNacientes, 0) AS CantidadNacientesOriginal,
    {exprNacientesFinal} AS CantidadNacientesFinal,
    f.Pendiente AS PendienteOriginal,
    COALESCE(NULLIF(e.PendienteAjustada, ''), f.Pendiente) AS PendienteFinal
FROM dbo.EvaluacionesTecnicas e
INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.DecisionTecnica = 'Califica'
  AND e.EstadoEvaluacion IN ('Evaluada – Califica', 'FinalizadaCalifica');";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PlanPagoGenerationContextDTO
        {
            IdEvaluacion = reader.GetInt32(reader.GetOrdinal("IdEvaluacion")),
            IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
            IdPropietario = reader.GetInt32(reader.GetOrdinal("IdPropietario")),
            NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
            HectareasOriginales = reader.GetDecimal(reader.GetOrdinal("HectareasOriginales")),
            HectareasAprobadas = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
            VegetacionOriginal = reader["VegetacionOriginal"]?.ToString() ?? string.Empty,
            VegetacionFinal = reader["VegetacionFinal"]?.ToString() ?? string.Empty,
            TieneRiosOQuebradasOriginal = reader.GetBoolean(reader.GetOrdinal("TieneRiosOQuebradasOriginal")),
            TieneRiosOQuebradasFinal = reader.GetBoolean(reader.GetOrdinal("TieneRiosOQuebradasFinal")),
            TieneRecursosHidricosFinal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
            CantidadNacientesOriginal = reader.GetInt32(reader.GetOrdinal("CantidadNacientesOriginal")),
            CantidadNacientesFinal = reader.GetInt32(reader.GetOrdinal("CantidadNacientesFinal")),
            PendienteOriginal = reader["PendienteOriginal"]?.ToString() ?? string.Empty,
            PendienteFinal = reader["PendienteFinal"]?.ToString() ?? string.Empty
        };
    }

    public async Task<PaymentConfigurationVersionDTO?> ObtenerConfiguracionVigenteParaAnioAsync(int anio)
    {
        const string sqlDetalle = @"
SELECT TipoFactor, ValorFactor, PorcentajeAjuste
FROM dbo.ConfiguracionPagoDetalle
WHERE IdConfiguracionPago = @IdConfiguracionPago;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var existeTopePorcentajeAjuste = await ExisteColumnaAsync(connection, "ConfiguracionesPago", "TopePorcentajeAjuste");
        var existePorcentajeTopeAjuste = await ExisteColumnaAsync(connection, "ConfiguracionesPago", "PorcentajeTopeAjuste");
        var expresionTope = existeTopePorcentajeAjuste
            ? "ISNULL(TopePorcentajeAjuste, 0)"
            : existePorcentajeTopeAjuste
                ? "ISNULL(PorcentajeTopeAjuste, 0)"
                : "CAST(0 AS decimal(5,2))";

        var sqlConfig = $@"
SELECT TOP 1 IdConfiguracionPago, Version, PrecioBasePorHectarea, {expresionTope} AS TopePorcentajeAjuste
FROM dbo.ConfiguracionesPago
WHERE Activa = 1
  AND FechaVigenciaDesde <= DATEFROMPARTS(@Anio, 1, 1)
  AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= DATEFROMPARTS(@Anio, 1, 1))
ORDER BY FechaVigenciaDesde DESC, IdConfiguracionPago DESC;";

        using var configCommand = new SqlCommand(sqlConfig, connection);
        configCommand.Parameters.AddWithValue("@Anio", anio);

        using var configReader = await configCommand.ExecuteReaderAsync();
        if (!await configReader.ReadAsync())
        {
            return null;
        }

        var config = new PaymentConfigurationVersionDTO
        {
            IdConfiguracionPago = configReader.GetInt32(configReader.GetOrdinal("IdConfiguracionPago")),
            Version = configReader["Version"] == DBNull.Value ? 0 : configReader.GetInt32(configReader.GetOrdinal("Version")),
            PrecioBasePorHectarea = configReader.GetDecimal(configReader.GetOrdinal("PrecioBasePorHectarea")),
            TopePorcentajeAjuste = configReader.GetDecimal(configReader.GetOrdinal("TopePorcentajeAjuste"))
        };

        await configReader.CloseAsync();

        using var detailCommand = new SqlCommand(sqlDetalle, connection);
        detailCommand.Parameters.AddWithValue("@IdConfiguracionPago", config.IdConfiguracionPago);

        using var detailReader = await detailCommand.ExecuteReaderAsync();
        while (await detailReader.ReadAsync())
        {
            var tipo = detailReader["TipoFactor"]?.ToString() ?? string.Empty;
            var valor = detailReader["ValorFactor"]?.ToString() ?? string.Empty;
            var porcentaje = detailReader.GetDecimal(detailReader.GetOrdinal("PorcentajeAjuste"));

            switch (tipo)
            {
                case "Vegetacion":
                    config.VegetacionAjustes[valor] = porcentaje;
                    break;
                case "RecursosHidricos":
                    config.HidricosAjustes[valor] = porcentaje;
                    break;
                case "Pendiente":
                    config.PendienteAjustes[valor] = porcentaje;
                    break;
            }
        }

        return config;
    }

    private static async Task<bool> ExisteColumnaAsync(SqlConnection connection, string tabla, string columna)
    {
        const string sql = @"
SELECT COUNT(1)
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = 'dbo'
  AND t.name = @Tabla
  AND c.name = @Columna;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Tabla", tabla);
        command.Parameters.AddWithValue("@Columna", columna);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }

    public async Task<PlanPagoDTO> CrearOActualizarPlanPreliminarAsync(
        PlanPagoGenerationContextDTO context,
        PaymentConfigurationVersionDTO config,
        PaymentCalculationResultDTO calculation,
        int anio)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var tx = connection.BeginTransaction();

        var existingPlanId = await ObtenerPlanExistenteAsync(connection, tx, context.IdFinca, anio);
        var estadoInicial = EstadosPlanPago.PendienteDatosBancarios;
        int idPlanPago;

        if (existingPlanId.HasValue)
        {
            throw new InvalidOperationException($"Ya existe un plan de pago para la finca {context.IdFinca} en el año {anio}. No se permite recalcular ni sobrescribir.");
        }

        idPlanPago = await InsertarPlanAsync(connection, tx, context, config, calculation, anio, estadoInicial);
        await UpsertDetalleCalculoAsync(connection, tx, idPlanPago, context, config, calculation);
        await InsertarCuotasAsync(connection, tx, idPlanPago, anio, calculation.MontoMensualTotal);

        await tx.CommitAsync();

        return new PlanPagoDTO
        {
            IdPlanPago = idPlanPago,
            IdFinca = context.IdFinca,
            NombreFinca = context.NombreFinca,
            Anio = anio,
            IdConfiguracionPago = config.IdConfiguracionPago,
            MontoBaseMensual = calculation.MontoBaseMensual,
            PorcentajeAjusteTotal = calculation.PorcentajeAjusteAplicado,
            MontoMensualCalculado = calculation.MontoMensualTotal,
            EstadoPlan = estadoInicial,
            FechaGeneracion = DateTime.UtcNow,
            DetalleCalculo = new PlanPagoCalculoDetalleDTO
            {
                IdConfiguracionPago = config.IdConfiguracionPago,
                VersionConfiguracionPago = config.Version,
                HectareasOriginales = context.HectareasOriginales,
                HectareasAprobadas = context.HectareasAprobadas,
                HectareasFinalesAprobadas = context.HectareasAprobadas,
                PrecioBasePorHectarea = config.PrecioBasePorHectarea,
                MontoBaseMensual = calculation.MontoBaseMensual,
                PorcentajeVegetacion = calculation.PorcentajeVegetacion,
                MontoAjusteVegetacion = calculation.MontoAjusteVegetacion,
                PorcentajeRiosQuebradas = calculation.PorcentajeRiosQuebradas,
                MontoAjusteRiosQuebradas = calculation.MontoAjusteRiosQuebradas,
                PorcentajeHidrico = calculation.PorcentajeHidrico,
                PorcentajeNacientes = calculation.PorcentajeNacientes,
                MontoAjusteNacientes = calculation.MontoAjusteNacientes,
                PorcentajePendiente = calculation.PorcentajePendiente,
                MontoAjustePendiente = calculation.MontoAjustePendiente,
                PorcentajeTotalAntesTope = calculation.PorcentajeAjusteTotalBruto,
                PorcentajeTopeAplicado = calculation.TopePorcentajeAjuste,
                PorcentajeTotalAplicado = calculation.PorcentajeAjusteAplicado,
                PorcentajeRecortadoPorTope = calculation.PorcentajeRecortadoPorTope,
                MontoAjusteMensual = calculation.MontoAjusteMensual,
                MontoAjusteBrutoMensual = calculation.MontoAjusteBrutoMensual,
                MontoRecortadoPorTope = calculation.MontoRecortadoPorTope,
                MontoFinalMensual = calculation.MontoMensualTotal,
                VegetacionOriginal = context.VegetacionOriginal,
                VegetacionFinal = context.VegetacionFinal,
                TieneRiosOQuebradasOriginal = context.TieneRiosOQuebradasOriginal,
                TieneRiosOQuebradasFinal = context.TieneRiosOQuebradasFinal,
                TieneRecursosHidricosFinal = context.TieneRecursosHidricosFinal,
                CantidadNacientesOriginal = context.CantidadNacientesOriginal,
                CantidadNacientesFinal = context.CantidadNacientesFinal,
                PendienteOriginal = context.PendienteOriginal,
                PendienteFinal = context.PendienteFinal
            }
        };
    }

    public async Task<bool> AsociarCuentaYPasarAPendienteAprobacionAsync(int idPlanPago, int idUsuario, int idCuentaBancaria)
    {
        const string sql = @"
UPDATE pp
SET pp.IdCuentaBancaria = @IdCuentaBancaria,
    pp.EstadoPlan = @EstadoPendienteAprobacion
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
WHERE pp.IdPlanPago = @IdPlanPago
  AND f.IdPropietario = @IdUsuario
  AND EXISTS (
      SELECT 1
      FROM dbo.CuentasBancarias cb
      WHERE cb.IdCuentaBancaria = @IdCuentaBancaria
        AND cb.IdUsuario = @IdUsuario
        AND cb.EstadoValidacion = 'Validada'
        AND cb.Activa = 1)
  AND pp.EstadoPlan IN (@EstadoPendienteDatos, @EstadoBorrador, @EstadoPendienteAprobacion);

SELECT @@ROWCOUNT;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
        command.Parameters.AddWithValue("@IdCuentaBancaria", idCuentaBancaria);
        command.Parameters.AddWithValue("@EstadoPendienteAprobacion", EstadosPlanPago.PendienteAprobacionFinal);
        command.Parameters.AddWithValue("@EstadoPendienteDatos", EstadosPlanPago.PendienteDatosBancarios);
        command.Parameters.AddWithValue("@EstadoBorrador", EstadosPlanPago.BorradorGenerado);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }

    public async Task<bool> AprobarPlanYActivarAsync(int idPlanPago, int idIngeniero)
    {
        const string sql = @"
UPDATE pp
SET pp.EstadoPlan = @EstadoActivo
FROM dbo.PlanesPago pp
INNER JOIN dbo.EvaluacionesTecnicas e ON e.IdEvaluacion = pp.IdEvaluacion
WHERE pp.IdPlanPago = @IdPlanPago
  AND pp.IdCuentaBancaria IS NOT NULL
  AND pp.EstadoPlan = @EstadoPendienteAprobacion
  AND e.IdIngeniero = @IdIngeniero;

SELECT @@ROWCOUNT;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        command.Parameters.AddWithValue("@IdIngeniero", idIngeniero);
        command.Parameters.AddWithValue("@EstadoPendienteAprobacion", EstadosPlanPago.PendienteAprobacionFinal);
        command.Parameters.AddWithValue("@EstadoActivo", EstadosPlanPago.Activo);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }

    public async Task<List<CuotaPlanPagoDTO>> ObtenerHistorialCuotasDuenoAsync(int idPropietario)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.SP_Pagos_ObtenerHistorialDueno", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@IdPropietario", idPropietario);

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<CuotaPlanPagoDTO>();
        while (await reader.ReadAsync())
        {
            resultado.Add(new CuotaPlanPagoDTO
            {
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdCuotaPago = reader.GetInt32(reader.GetOrdinal("IdCuotaPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                Mes = reader.GetInt32(reader.GetOrdinal("Mes")),
                FechaProgramada = reader.GetDateTime(reader.GetOrdinal("FechaProgramada")),
                MontoProgramado = reader.GetDecimal(reader.GetOrdinal("MontoProgramado")),
                MontoPendiente = reader.GetDecimal(reader.GetOrdinal("MontoPendiente")),
                EstadoCuota = reader["EstadoCuota"]?.ToString() ?? string.Empty,
                FechaPago = reader["FechaPago"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaPago"))
            });
        }

        return resultado;
    }

    public async Task<List<PlanPagoResumenDTO>> ObtenerPlanesDuenoAsync(int idPropietario)
    {
        const string sql = @"
SELECT
    pp.IdPlanPago,
    pp.IdFinca,
    f.NombreFinca,
    pp.Anio,
    pp.MontoMensualCalculado,
    CAST(pp.MontoMensualCalculado * 12 AS DECIMAL(12,2)) AS MontoAnualEstimado,
    pp.EstadoPlan,
    pp.IdCuentaBancaria
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
WHERE f.IdPropietario = @IdPropietario
ORDER BY pp.Anio DESC, pp.IdPlanPago DESC;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdPropietario", idPropietario);

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<PlanPagoResumenDTO>();
        while (await reader.ReadAsync())
        {
            resultado.Add(new PlanPagoResumenDTO
            {
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                MontoMensualCalculado = reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
                MontoAnualEstimado = reader.GetDecimal(reader.GetOrdinal("MontoAnualEstimado")),
                EstadoPlan = reader["EstadoPlan"]?.ToString() ?? string.Empty,
                IdCuentaBancaria = reader["IdCuentaBancaria"] == DBNull.Value
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria"))
            });
        }

        return resultado;
    }

    public async Task<List<PlanPagoResumenDTO>> ObtenerPlanesPendientesAprobacionIngenieroAsync(int idIngeniero)
    {
        return await ObtenerPlanesConFiltrosAsync(new FiltroPlanesPagoDTO
        {
            IdIngeniero = idIngeniero > 0 ? idIngeniero : null,
            SoloPendientes = true
        });
    }

    public async Task<List<PlanPagoResumenDTO>> ObtenerPlanesConFiltrosAsync(FiltroPlanesPagoDTO filtro)
    {
        filtro ??= new FiltroPlanesPagoDTO();

        var condiciones = new List<string>();
        var parametros = new List<SqlParameter>();

        if (filtro.IdPropietario.HasValue && filtro.IdPropietario.Value > 0)
        {
            condiciones.Add("f.IdPropietario = @IdPropietario");
            parametros.Add(new SqlParameter("@IdPropietario", filtro.IdPropietario.Value));
        }

        if (filtro.IdIngeniero.HasValue && filtro.IdIngeniero.Value > 0)
        {
            condiciones.Add("e.IdIngeniero = @IdIngeniero");
            parametros.Add(new SqlParameter("@IdIngeniero", filtro.IdIngeniero.Value));
        }

        if (filtro.IdFinca.HasValue && filtro.IdFinca.Value > 0)
        {
            condiciones.Add("pp.IdFinca = @IdFinca");
            parametros.Add(new SqlParameter("@IdFinca", filtro.IdFinca.Value));
        }

        if (filtro.Anio.HasValue && filtro.Anio.Value >= 2000 && filtro.Anio.Value <= 2100)
        {
            condiciones.Add("pp.Anio = @Anio");
            parametros.Add(new SqlParameter("@Anio", filtro.Anio.Value));
        }

        if (filtro.SoloPendientes)
        {
            condiciones.Add("pp.EstadoPlan IN (@EstadoPendienteDatos, @EstadoPendienteAprobacion)");
            parametros.Add(new SqlParameter("@EstadoPendienteDatos", EstadosPlanPago.PendienteDatosBancarios));
            parametros.Add(new SqlParameter("@EstadoPendienteAprobacion", EstadosPlanPago.PendienteAprobacionFinal));
        }
        else if (!string.IsNullOrWhiteSpace(filtro.EstadoPlan))
        {
            condiciones.Add("pp.EstadoPlan = @EstadoPlan");
            parametros.Add(new SqlParameter("@EstadoPlan", filtro.EstadoPlan.Trim()));
        }

        var where = condiciones.Count > 0
            ? $"WHERE {string.Join(" AND ", condiciones)}"
            : string.Empty;

        var sql = $@"
SELECT
    pp.IdPlanPago,
    pp.IdFinca,
    f.NombreFinca,
    pp.Anio,
    pp.MontoMensualCalculado,
    CAST(pp.MontoMensualCalculado * 12 AS DECIMAL(12,2)) AS MontoAnualEstimado,
    pp.EstadoPlan,
    pp.IdCuentaBancaria
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
LEFT JOIN dbo.EvaluacionesTecnicas e ON e.IdEvaluacion = pp.IdEvaluacion
{where}
ORDER BY pp.Anio DESC, pp.IdPlanPago DESC;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand(sql, connection);
        if (parametros.Count > 0)
        {
            command.Parameters.AddRange(parametros.ToArray());
        }

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<PlanPagoResumenDTO>();
        while (await reader.ReadAsync())
        {
            resultado.Add(new PlanPagoResumenDTO
            {
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                MontoMensualCalculado = reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
                MontoAnualEstimado = reader.GetDecimal(reader.GetOrdinal("MontoAnualEstimado")),
                EstadoPlan = reader["EstadoPlan"]?.ToString() ?? string.Empty,
                IdCuentaBancaria = reader["IdCuentaBancaria"] == DBNull.Value
                    ? null
                    : reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria"))
            });
        }

        return resultado;
    }

    public async Task<List<CuentaBancariaDuenoDTO>> ObtenerCuentasBancariasDuenoAsync(int idUsuario)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.SP_Pagos_ObtenerCuentasBancariasDueno", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<CuentaBancariaDuenoDTO>();
        while (await reader.ReadAsync())
        {
            resultado.Add(new CuentaBancariaDuenoDTO
            {
                IdCuentaBancaria = reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria")),
                Banco = reader["Banco"]?.ToString() ?? string.Empty,
                NumeroCuenta = reader["NumeroCuenta"]?.ToString() ?? string.Empty,
                TipoCuenta = reader["TipoCuenta"]?.ToString() ?? string.Empty,
                Titular = reader["Titular"]?.ToString() ?? string.Empty,
                EstadoValidacion = reader["EstadoValidacion"]?.ToString() ?? string.Empty,
                Activa = reader.GetBoolean(reader.GetOrdinal("Activa")),
                FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaRegistro"))
            });
        }

        return resultado;
    }

    public async Task<int> RegistrarCuentaBancariaDuenoAsync(RegistrarCuentaBancariaDTO dto)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.SP_Pagos_RegistrarCuentaBancariaDueno", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@IdUsuario", dto.IdUsuario);
        command.Parameters.AddWithValue("@Banco", dto.Banco.Trim());
        command.Parameters.AddWithValue("@NumeroCuenta", dto.NumeroCuenta.Trim());
        command.Parameters.AddWithValue("@TipoCuenta", dto.TipoCuenta.Trim());
        command.Parameters.AddWithValue("@Titular", dto.Titular.Trim());

        try
        {
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result ?? 0);
        }
        catch (SqlException ex) when (ex.Message.Contains("CK_Cuentas_TipoCuenta", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El tipo de cuenta no es válido. Use: Ahorro, Corriente, IBAN, SINPE u Otra.", ex);
        }
    }

    public async Task<List<OwnerPaymentPlanDto>> ObtenerPlanesOwnerAsync(int idPropietario)
    {
        const string sql = @"
SELECT
    pp.IdPlanPago,
    pp.IdFinca,
    f.NombreFinca,
    pp.Anio,
    pp.EstadoPlan,
    pp.IdCuentaBancaria,
    pp.MontoMensualCalculado,
    CAST(pp.MontoMensualCalculado * 12 AS DECIMAL(12,2)) AS MontoAnual,
    cb.EstadoValidacion AS EstadoCuentaBancaria,
    cb.NumeroCuenta,
    dc.HectareasAprobadas,
    dc.VegetacionFinal,
    dc.PorcentajeTotalAplicado,
    dc.PorcentajeTopeAplicado,
    dc.PorcentajeTotalAntesTope
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
LEFT JOIN dbo.CuentasBancarias cb ON cb.IdCuentaBancaria = pp.IdCuentaBancaria
LEFT JOIN dbo.PlanesPagoDetalleCalculo dc ON dc.IdPlanPago = pp.IdPlanPago
WHERE f.IdPropietario = @IdPropietario
ORDER BY pp.Anio DESC, pp.IdPlanPago DESC;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdPropietario", idPropietario);

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<OwnerPaymentPlanDto>();
        while (await reader.ReadAsync())
        {
            var cuenta = reader["NumeroCuenta"]?.ToString();
            var porcentajeTope = reader["PorcentajeTopeAplicado"] == DBNull.Value ? 0 : reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAplicado"));
            var porcentajeAntesTope = reader["PorcentajeTotalAntesTope"] == DBNull.Value ? 0 : reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope"));
            resultado.Add(new OwnerPaymentPlanDto
            {
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                EstadoPlan = reader["EstadoPlan"]?.ToString() ?? string.Empty,
                IdCuentaBancaria = reader["IdCuentaBancaria"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria")),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
                MontoAnual = reader.GetDecimal(reader.GetOrdinal("MontoAnual")),
                EstadoCuentaBancaria = reader["EstadoCuentaBancaria"]?.ToString() ?? "Pendiente",
                CuentaBancariaMascara = MascaraCuenta(cuenta),
                ResumenCalculo = new OwnerPaymentCalculationSummaryDto
                {
                    HectareasAprobadas = reader["HectareasAprobadas"] == DBNull.Value ? 0 : reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
                    CoberturaVegetacion = reader["VegetacionFinal"]?.ToString() ?? "No disponible",
                    AjusteAplicadoPorcentaje = reader["PorcentajeTotalAplicado"] == DBNull.Value ? 0 : reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAplicado")),
                    TopeAplicadoPorcentaje = porcentajeTope,
                    SeAplicoTope = porcentajeAntesTope > porcentajeTope && porcentajeTope > 0
                }
            });
        }

        return resultado;
    }

    public async Task<OwnerPaymentPlanDetailDto?> ObtenerDetalleOwnerAsync(int idPropietario, int idPlanPago)
    {
        var planes = await ObtenerPlanesOwnerAsync(idPropietario);
        var plan = planes.FirstOrDefault(p => p.IdPlanPago == idPlanPago);
        if (plan == null)
        {
            return null;
        }

        return new OwnerPaymentPlanDetailDto
        {
            Plan = plan,
            Calculo = await ObtenerDetalleCalculoPorPlanAsync(idPlanPago),
            Cuotas = await ObtenerCuotasPorPlanAsync(idPlanPago)
        };
    }

    public async Task<EngineerPaymentImpactDto?> ObtenerImpactoPagoIngenieroAsync(int idIngeniero, int idEvaluacion)
    {
        const string sql = @"
SELECT TOP 1
    e.IdEvaluacion,
    f.IdFinca,
    f.NombreFinca,
    e.DecisionTecnica,
    pp.IdPlanPago,
    pp.EstadoPlan,
    pp.MontoMensualCalculado,
    CAST(pp.MontoMensualCalculado * 12 AS DECIMAL(12,2)) AS MontoAnual,
    cb.EstadoValidacion AS EstadoCuentaBancaria,
    pp.IdCuentaBancaria
FROM dbo.EvaluacionesTecnicas e
INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
LEFT JOIN dbo.PlanesPago pp ON pp.IdEvaluacion = e.IdEvaluacion
LEFT JOIN dbo.CuentasBancarias cb ON cb.IdCuentaBancaria = pp.IdCuentaBancaria
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.IdIngeniero = @IdIngeniero;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
        command.Parameters.AddWithValue("@IdIngeniero", idIngeniero);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var decision = reader["DecisionTecnica"]?.ToString() ?? string.Empty;
        int? idPlan = reader["IdPlanPago"] == DBNull.Value
            ? null
            : reader.GetInt32(reader.GetOrdinal("IdPlanPago"));
        var estadoPlan = reader["EstadoPlan"]?.ToString() ?? string.Empty;
        var estadoCuenta = reader["EstadoCuentaBancaria"]?.ToString() ?? "Pendiente";

        return new EngineerPaymentImpactDto
        {
            IdEvaluacion = reader.GetInt32(reader.GetOrdinal("IdEvaluacion")),
            IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
            NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
            DecisionTecnica = decision,
            GeneroPlan = idPlan.HasValue,
            IdPlanPago = idPlan,
            EstadoPlan = estadoPlan,
            EstadoContinuidad = ResolverEstadoContinuidad(decision, idPlan.HasValue, estadoPlan, estadoCuenta),
            MontoMensualReferencial = reader["MontoMensualCalculado"] == DBNull.Value ? null : reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
            MontoAnualReferencial = reader["MontoAnual"] == DBNull.Value ? null : reader.GetDecimal(reader.GetOrdinal("MontoAnual")),
            EstadoCuentaBancaria = estadoCuenta,
            CuentaRegistrada = reader["IdCuentaBancaria"] != DBNull.Value,
            CuentaValidada = string.Equals(estadoCuenta, "Validada", StringComparison.OrdinalIgnoreCase)
        };
    }

    public async Task<List<AdminPaymentPlanDto>> ObtenerPlanesAdminAsync(AdminPaymentPlanFilterDto filtro)
    {
        filtro ??= new AdminPaymentPlanFilterDto();
        var condiciones = new List<string>();
        var parametros = new List<SqlParameter>();

        if (filtro.Anio.HasValue)
        {
            condiciones.Add("pp.Anio = @Anio");
            parametros.Add(new SqlParameter("@Anio", filtro.Anio.Value));
        }

        if (filtro.IdFinca.HasValue)
        {
            condiciones.Add("pp.IdFinca = @IdFinca");
            parametros.Add(new SqlParameter("@IdFinca", filtro.IdFinca.Value));
        }

        if (filtro.IdPropietario.HasValue)
        {
            condiciones.Add("f.IdPropietario = @IdPropietario");
            parametros.Add(new SqlParameter("@IdPropietario", filtro.IdPropietario.Value));
        }

        if (filtro.IdIngeniero.HasValue)
        {
            condiciones.Add("e.IdIngeniero = @IdIngeniero");
            parametros.Add(new SqlParameter("@IdIngeniero", filtro.IdIngeniero.Value));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Provincia))
        {
            condiciones.Add("f.Provincia = @Provincia");
            parametros.Add(new SqlParameter("@Provincia", filtro.Provincia.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Canton))
        {
            condiciones.Add("f.Canton = @Canton");
            parametros.Add(new SqlParameter("@Canton", filtro.Canton.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Distrito))
        {
            condiciones.Add("f.Distrito = @Distrito");
            parametros.Add(new SqlParameter("@Distrito", filtro.Distrito.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.EstadoPlan))
        {
            condiciones.Add("pp.EstadoPlan = @EstadoPlan");
            parametros.Add(new SqlParameter("@EstadoPlan", filtro.EstadoPlan.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.EstadoBancario))
        {
            condiciones.Add("COALESCE(cb.EstadoValidacion, 'Pendiente') = @EstadoBancario");
            parametros.Add(new SqlParameter("@EstadoBancario", filtro.EstadoBancario.Trim()));
        }

        var where = condiciones.Count > 0 ? $"WHERE {string.Join(" AND ", condiciones)}" : string.Empty;
        var sql = $@"
SELECT
    pp.IdPlanPago,
    pp.IdFinca,
    f.NombreFinca,
    f.Provincia,
    f.Canton,
    f.Distrito,
    pp.Anio,
    pp.EstadoPlan,
    pp.MontoMensualCalculado,
    CAST(pp.MontoMensualCalculado * 12 AS DECIMAL(12,2)) AS MontoAnual,
    COALESCE(cb.EstadoValidacion, 'Pendiente') AS EstadoBancario,
    cb.NumeroCuenta,
    cp.Version AS VersionConfiguracion,
    prop.NombreCompleto AS Propietario,
    e.IdIngeniero,
    COALESCE(ing.NombreCompleto, 'Sin asignar') AS Ingeniero
FROM dbo.PlanesPago pp
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
INNER JOIN dbo.Usuarios prop ON prop.IdUsuario = f.IdPropietario
INNER JOIN dbo.ConfiguracionesPago cp ON cp.IdConfiguracionPago = pp.IdConfiguracionPago
LEFT JOIN dbo.CuentasBancarias cb ON cb.IdCuentaBancaria = pp.IdCuentaBancaria
LEFT JOIN dbo.EvaluacionesTecnicas e ON e.IdEvaluacion = pp.IdEvaluacion
LEFT JOIN dbo.Usuarios ing ON ing.IdUsuario = e.IdIngeniero
{where}
ORDER BY pp.Anio DESC, pp.IdPlanPago DESC;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        if (parametros.Count > 0)
        {
            command.Parameters.AddRange(parametros.ToArray());
        }

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<AdminPaymentPlanDto>();
        while (await reader.ReadAsync())
        {
            resultado.Add(new AdminPaymentPlanDto
            {
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Propietario = reader["Propietario"]?.ToString() ?? string.Empty,
                IdIngeniero = reader["IdIngeniero"] == DBNull.Value ? null : reader.GetInt32(reader.GetOrdinal("IdIngeniero")),
                Ingeniero = reader["Ingeniero"]?.ToString() ?? string.Empty,
                Provincia = reader["Provincia"]?.ToString() ?? string.Empty,
                Canton = reader["Canton"]?.ToString() ?? string.Empty,
                Distrito = reader["Distrito"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                EstadoPlan = reader["EstadoPlan"]?.ToString() ?? string.Empty,
                EstadoBancario = reader["EstadoBancario"]?.ToString() ?? "Pendiente",
                CuentaBancariaMascara = MascaraCuenta(reader["NumeroCuenta"]?.ToString()),
                MontoMensual = reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
                MontoAnual = reader.GetDecimal(reader.GetOrdinal("MontoAnual")),
                VersionConfiguracion = reader.GetInt32(reader.GetOrdinal("VersionConfiguracion"))
            });
        }

        return resultado;
    }

    public async Task<AdminPaymentPlanDetailDto?> ObtenerDetalleAdminAsync(int idPlanPago)
    {
        var plan = (await ObtenerPlanesAdminAsync(new AdminPaymentPlanFilterDto()))
            .FirstOrDefault(p => p.IdPlanPago == idPlanPago);
        if (plan == null)
        {
            return null;
        }

        const string sqlBitacora = @"
SELECT TOP 20 FechaAccion, Accion, Detalle, IdUsuario
FROM dbo.AuditoriaLog
WHERE Modulo = 'Pagos'
  AND IdRegistroAfectado = @IdPlanPago
ORDER BY FechaAccion DESC;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var calculo = await ObtenerDetalleCalculoPorPlanAsync(idPlanPago) ?? new PlanPagoCalculoDetalleDTO();

        var bitacora = new List<AuditoriaPlanPagoDto>();
        using var commandBitacora = new SqlCommand(sqlBitacora, connection);
        commandBitacora.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        using var readerBitacora = await commandBitacora.ExecuteReaderAsync();
        while (await readerBitacora.ReadAsync())
        {
            bitacora.Add(new AuditoriaPlanPagoDto
            {
                FechaAccion = readerBitacora.GetDateTime(readerBitacora.GetOrdinal("FechaAccion")),
                Accion = readerBitacora["Accion"]?.ToString() ?? string.Empty,
                Detalle = readerBitacora["Detalle"]?.ToString(),
                IdUsuario = readerBitacora["IdUsuario"] == DBNull.Value ? null : readerBitacora.GetInt32(readerBitacora.GetOrdinal("IdUsuario"))
            });
        }

        return new AdminPaymentPlanDetailDto
        {
            Plan = plan,
            Calculo = calculo,
            Cuotas = await ObtenerCuotasPorPlanAsync(idPlanPago),
            Bitacora = bitacora
        };
    }

    private async Task<List<CuotaPlanPagoDTO>> ObtenerCuotasPorPlanAsync(int idPlanPago)
    {
        const string sql = @"
SELECT pp.IdPlanPago, c.IdCuotaPago, pp.IdFinca, f.NombreFinca, pp.Anio, c.Mes, c.FechaProgramada, c.MontoProgramado, c.MontoPendiente, c.EstadoCuota, c.FechaPago
FROM dbo.CuotasPago c
INNER JOIN dbo.PlanesPago pp ON pp.IdPlanPago = c.IdPlanPago
INNER JOIN dbo.Fincas f ON f.IdFinca = pp.IdFinca
WHERE c.IdPlanPago = @IdPlanPago
ORDER BY c.Mes;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        using var reader = await command.ExecuteReaderAsync();

        var cuotas = new List<CuotaPlanPagoDTO>();
        while (await reader.ReadAsync())
        {
            cuotas.Add(new CuotaPlanPagoDTO
            {
                IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
                IdCuotaPago = reader.GetInt32(reader.GetOrdinal("IdCuotaPago")),
                IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
                NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                Mes = reader.GetInt32(reader.GetOrdinal("Mes")),
                FechaProgramada = reader.GetDateTime(reader.GetOrdinal("FechaProgramada")),
                MontoProgramado = reader.GetDecimal(reader.GetOrdinal("MontoProgramado")),
                MontoPendiente = reader.GetDecimal(reader.GetOrdinal("MontoPendiente")),
                EstadoCuota = reader["EstadoCuota"]?.ToString() ?? string.Empty,
                FechaPago = reader["FechaPago"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaPago"))
            });
        }

        return cuotas;
    }

    private async Task<PlanPagoCalculoDetalleDTO?> ObtenerDetalleCalculoPorPlanAsync(int idPlanPago)
    {
        const string sql = @"SELECT TOP 1 * FROM dbo.PlanesPagoDetalleCalculo WHERE IdPlanPago = @IdPlanPago;";
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var baseMensual = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual"));
        var porcentajeBruto = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope"));
        var montoBruto = Math.Round(baseMensual * (porcentajeBruto / 100m), 2, MidpointRounding.AwayFromZero);
        var montoAplicado = reader.GetDecimal(reader.GetOrdinal("MontoAjusteMensual"));
        return new PlanPagoCalculoDetalleDTO
        {
            HectareasOriginales = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
            HectareasAprobadas = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
            HectareasFinalesAprobadas = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
            PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
            MontoBaseMensual = baseMensual,
            PorcentajeVegetacion = reader.GetDecimal(reader.GetOrdinal("PorcentajeVegetacion")),
            MontoAjusteVegetacion = Math.Round(baseMensual * (reader.GetDecimal(reader.GetOrdinal("PorcentajeVegetacion")) / 100m), 2, MidpointRounding.AwayFromZero),
            PorcentajeRiosQuebradas = reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")),
            MontoAjusteRiosQuebradas = Math.Round(baseMensual * (reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")) / 100m), 2, MidpointRounding.AwayFromZero),
            PorcentajeHidrico = reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")),
            PorcentajeNacientes = reader.GetDecimal(reader.GetOrdinal("PorcentajeNacientes")),
            MontoAjusteNacientes = Math.Round(baseMensual * (reader.GetDecimal(reader.GetOrdinal("PorcentajeNacientes")) / 100m), 2, MidpointRounding.AwayFromZero),
            PorcentajePendiente = reader.GetDecimal(reader.GetOrdinal("PorcentajePendiente")),
            MontoAjustePendiente = Math.Round(baseMensual * (reader.GetDecimal(reader.GetOrdinal("PorcentajePendiente")) / 100m), 2, MidpointRounding.AwayFromZero),
            PorcentajeTotalAntesTope = porcentajeBruto,
            PorcentajeTopeAplicado = reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAplicado")),
            PorcentajeTotalAplicado = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAplicado")),
            PorcentajeRecortadoPorTope = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope")) - reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAplicado")),
            MontoAjusteMensual = montoAplicado,
            MontoAjusteBrutoMensual = montoBruto,
            MontoRecortadoPorTope = Math.Round(montoBruto - montoAplicado, 2, MidpointRounding.AwayFromZero),
            MontoFinalMensual = reader.GetDecimal(reader.GetOrdinal("MontoFinalMensual")),
            VegetacionOriginal = reader["VegetacionFinal"]?.ToString() ?? string.Empty,
            VegetacionFinal = reader["VegetacionFinal"]?.ToString() ?? string.Empty,
            TieneRiosOQuebradasOriginal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
            TieneRiosOQuebradasFinal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
            TieneRecursosHidricosFinal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
            CantidadNacientesOriginal = reader.GetInt32(reader.GetOrdinal("CantidadNacientesFinal")),
            CantidadNacientesFinal = reader.GetInt32(reader.GetOrdinal("CantidadNacientesFinal")),
            PendienteOriginal = reader["PendienteFinal"]?.ToString() ?? string.Empty,
            PendienteFinal = reader["PendienteFinal"]?.ToString() ?? string.Empty
        };
    }

    private static string? MascaraCuenta(string? numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
        {
            return null;
        }

        var compacta = new string(numeroCuenta.Where(char.IsLetterOrDigit).ToArray());
        if (compacta.Length <= 4)
        {
            return $"****{compacta}";
        }

        return $"****{compacta[^4..]}";
    }

    private static string ResolverEstadoContinuidad(string decisionTecnica, bool generoPlan, string estadoPlan, string estadoCuenta)
    {
        if (!string.Equals(decisionTecnica, "Califica", StringComparison.OrdinalIgnoreCase))
        {
            return "No califica";
        }

        if (!generoPlan)
        {
            return "Pendiente de generación";
        }

        if (!string.Equals(estadoCuenta, "Validada", StringComparison.OrdinalIgnoreCase))
        {
            return "Bloqueado por cuenta bancaria";
        }

        return string.Equals(estadoPlan, EstadosPlanPago.Activo, StringComparison.OrdinalIgnoreCase)
            ? "Plan generado correctamente"
            : "Pendiente de continuidad";
    }

    private static PlanPagoDTO MapPlanPago(SqlDataReader reader)
    {
        return new PlanPagoDTO
        {
            IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
            IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
            NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
            Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
            IdConfiguracionPago = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPago")),
            IdCuentaBancaria = reader["IdCuentaBancaria"] == DBNull.Value
                ? null
                : reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria")),
            MontoBaseMensual = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")),
            PorcentajeAjusteTotal = reader.GetDecimal(reader.GetOrdinal("PorcentajeAjusteTotal")),
            MontoMensualCalculado = reader.GetDecimal(reader.GetOrdinal("MontoMensualCalculado")),
            EstadoPlan = reader["EstadoPlan"]?.ToString() ?? string.Empty,
            FechaGeneracion = reader.GetDateTime(reader.GetOrdinal("FechaGeneracion")),
            DetalleCalculo = new PlanPagoCalculoDetalleDTO
            {
                HectareasAprobadas = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
                HectareasFinalesAprobadas = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
                PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
                MontoBaseMensual = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")),
                PorcentajeVegetacion = reader.GetDecimal(reader.GetOrdinal("PorcentajeVegetacion")),
                MontoAjusteVegetacion = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")) * reader.GetDecimal(reader.GetOrdinal("PorcentajeVegetacion")) / 100m,
                PorcentajeRiosQuebradas = reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")),
                MontoAjusteRiosQuebradas = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")) * reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")) / 100m,
                PorcentajeHidrico = reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")),
                PorcentajeNacientes = reader.GetDecimal(reader.GetOrdinal("PorcentajeNacientes")),
                MontoAjusteNacientes = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")) * reader.GetDecimal(reader.GetOrdinal("PorcentajeNacientes")) / 100m,
                PorcentajePendiente = reader.GetDecimal(reader.GetOrdinal("PorcentajePendiente")),
                MontoAjustePendiente = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")) * reader.GetDecimal(reader.GetOrdinal("PorcentajePendiente")) / 100m,
                PorcentajeTotalAntesTope = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope")),
                PorcentajeTopeAplicado = reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAplicado")),
                PorcentajeTotalAplicado = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAplicado")),
                PorcentajeRecortadoPorTope = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope")) - reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAplicado")),
                MontoAjusteMensual = reader.GetDecimal(reader.GetOrdinal("MontoAjusteMensual")),
                MontoAjusteBrutoMensual = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")) * reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope")) / 100m,
                MontoRecortadoPorTope = (reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")) * reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope")) / 100m) - reader.GetDecimal(reader.GetOrdinal("MontoAjusteMensual")),
                MontoFinalMensual = reader.GetDecimal(reader.GetOrdinal("MontoFinalMensual")),
                VegetacionFinal = reader["VegetacionFinal"]?.ToString() ?? string.Empty,
                TieneRiosOQuebradasFinal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
                TieneRecursosHidricosFinal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
                CantidadNacientesFinal = reader.GetInt32(reader.GetOrdinal("CantidadNacientesFinal")),
                PendienteFinal = reader["PendienteFinal"]?.ToString() ?? string.Empty
            }
        };
    }

    private static async Task<int?> ObtenerPlanExistenteAsync(SqlConnection connection, SqlTransaction tx, int idFinca, int anio)
    {
        const string sql = @"SELECT TOP 1 IdPlanPago FROM dbo.PlanesPago WHERE IdFinca = @IdFinca AND Anio = @Anio ORDER BY IdPlanPago DESC;";
        using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddWithValue("@IdFinca", idFinca);
        command.Parameters.AddWithValue("@Anio", anio);
        var value = await command.ExecuteScalarAsync();
        return value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
    }

    private static async Task<int> InsertarPlanAsync(SqlConnection connection, SqlTransaction tx, PlanPagoGenerationContextDTO context, PaymentConfigurationVersionDTO config, PaymentCalculationResultDTO calculation, int anio, string estado)
    {
        const string sql = @"
INSERT INTO dbo.PlanesPago
(
    IdFinca, IdEvaluacion, IdConfiguracionPago, IdCuentaBancaria, Anio,
    MontoBaseMensual, PorcentajeAjusteTotal, MontoMensualCalculado, EstadoPlan
)
VALUES
(
    @IdFinca, @IdEvaluacion, @IdConfiguracionPago, NULL, @Anio,
    @MontoBaseMensual, @PorcentajeAjusteTotal, @MontoMensualCalculado, @EstadoPlan
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddWithValue("@IdFinca", context.IdFinca);
        command.Parameters.AddWithValue("@IdEvaluacion", context.IdEvaluacion);
        command.Parameters.AddWithValue("@IdConfiguracionPago", config.IdConfiguracionPago);
        command.Parameters.AddWithValue("@Anio", anio);
        command.Parameters.AddWithValue("@MontoBaseMensual", calculation.MontoBaseMensual);
        command.Parameters.AddWithValue("@PorcentajeAjusteTotal", calculation.PorcentajeAjusteAplicado);
        command.Parameters.AddWithValue("@MontoMensualCalculado", calculation.MontoMensualTotal);
        command.Parameters.AddWithValue("@EstadoPlan", estado);

        var value = await command.ExecuteScalarAsync();
        return Convert.ToInt32(value ?? 0);
    }

    private static async Task ActualizarPlanAsync(SqlConnection connection, SqlTransaction tx, int idPlanPago, PlanPagoGenerationContextDTO context, PaymentConfigurationVersionDTO config, PaymentCalculationResultDTO calculation, string estado)
    {
        const string sql = @"
UPDATE dbo.PlanesPago
SET IdEvaluacion = @IdEvaluacion,
    IdConfiguracionPago = @IdConfiguracionPago,
    IdCuentaBancaria = NULL,
    MontoBaseMensual = @MontoBaseMensual,
    PorcentajeAjusteTotal = @PorcentajeAjusteTotal,
    MontoMensualCalculado = @MontoMensualCalculado,
    EstadoPlan = @EstadoPlan
WHERE IdPlanPago = @IdPlanPago;";

        using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        command.Parameters.AddWithValue("@IdEvaluacion", context.IdEvaluacion);
        command.Parameters.AddWithValue("@IdConfiguracionPago", config.IdConfiguracionPago);
        command.Parameters.AddWithValue("@MontoBaseMensual", calculation.MontoBaseMensual);
        command.Parameters.AddWithValue("@PorcentajeAjusteTotal", calculation.PorcentajeAjusteAplicado);
        command.Parameters.AddWithValue("@MontoMensualCalculado", calculation.MontoMensualTotal);
        command.Parameters.AddWithValue("@EstadoPlan", estado);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task UpsertDetalleCalculoAsync(SqlConnection connection, SqlTransaction tx, int idPlanPago, PlanPagoGenerationContextDTO context, PaymentConfigurationVersionDTO config, PaymentCalculationResultDTO calculation)
    {
        const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.PlanesPagoDetalleCalculo WHERE IdPlanPago = @IdPlanPago)
BEGIN
    UPDATE dbo.PlanesPagoDetalleCalculo
    SET HectareasAprobadas = @HectareasAprobadas,
        PrecioBasePorHectarea = @PrecioBasePorHectarea,
        PorcentajeVegetacion = @PorcentajeVegetacion,
        MontoAjusteVegetacion = @MontoAjusteVegetacion,
        PorcentajeRiosQuebradas = @PorcentajeRiosQuebradas,
        MontoAjusteRiosQuebradas = @MontoAjusteRiosQuebradas,
        PorcentajeHidrico = @PorcentajeHidrico,
        PorcentajeNacientes = @PorcentajeNacientes,
        MontoAjusteNacientes = @MontoAjusteNacientes,
        PorcentajePendiente = @PorcentajePendiente,
        MontoAjustePendiente = @MontoAjustePendiente,
        PorcentajeTotalAntesTope = @PorcentajeTotalAntesTope,
        PorcentajeTopeAplicado = @PorcentajeTopeAplicado,
        PorcentajeTotalAplicado = @PorcentajeTotalAplicado,
        PorcentajeRecortadoPorTope = @PorcentajeRecortadoPorTope,
        MontoBaseMensual = @MontoBaseMensual,
        MontoAjusteBrutoMensual = @MontoAjusteBrutoMensual,
        MontoAjusteMensual = @MontoAjusteMensual,
        MontoRecortadoPorTope = @MontoRecortadoPorTope,
        MontoFinalMensual = @MontoFinalMensual,
        VegetacionFinal = @VegetacionFinal,
        TieneRecursosHidricosFinal = @TieneRecursosHidricosFinal,
        CantidadNacientesFinal = @CantidadNacientesFinal,
        PendienteFinal = @PendienteFinal
    WHERE IdPlanPago = @IdPlanPago;
END
ELSE
BEGIN
    INSERT INTO dbo.PlanesPagoDetalleCalculo
    (
        IdPlanPago, HectareasAprobadas, PrecioBasePorHectarea,
        PorcentajeVegetacion, MontoAjusteVegetacion, PorcentajeRiosQuebradas, MontoAjusteRiosQuebradas,
        PorcentajeHidrico, PorcentajeNacientes, MontoAjusteNacientes, PorcentajePendiente, MontoAjustePendiente,
        PorcentajeTotalAntesTope, PorcentajeTopeAplicado, PorcentajeTotalAplicado, PorcentajeRecortadoPorTope,
        MontoBaseMensual, MontoAjusteBrutoMensual, MontoAjusteMensual, MontoRecortadoPorTope, MontoFinalMensual,
        VegetacionFinal, TieneRecursosHidricosFinal, CantidadNacientesFinal, PendienteFinal
    )
    VALUES
    (
        @IdPlanPago, @HectareasAprobadas, @PrecioBasePorHectarea,
        @PorcentajeVegetacion, @MontoAjusteVegetacion, @PorcentajeRiosQuebradas, @MontoAjusteRiosQuebradas,
        @PorcentajeHidrico, @PorcentajeNacientes, @MontoAjusteNacientes, @PorcentajePendiente, @MontoAjustePendiente,
        @PorcentajeTotalAntesTope, @PorcentajeTopeAplicado, @PorcentajeTotalAplicado, @PorcentajeRecortadoPorTope,
        @MontoBaseMensual, @MontoAjusteBrutoMensual, @MontoAjusteMensual, @MontoRecortadoPorTope, @MontoFinalMensual,
        @VegetacionFinal, @TieneRecursosHidricosFinal, @CantidadNacientesFinal, @PendienteFinal
    );
END";

        using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        command.Parameters.AddWithValue("@HectareasAprobadas", context.HectareasAprobadas);
        command.Parameters.AddWithValue("@PrecioBasePorHectarea", config.PrecioBasePorHectarea);
        command.Parameters.AddWithValue("@PorcentajeVegetacion", calculation.PorcentajeVegetacion);
        command.Parameters.AddWithValue("@MontoAjusteVegetacion", calculation.MontoAjusteVegetacion);
        command.Parameters.AddWithValue("@PorcentajeRiosQuebradas", calculation.PorcentajeRiosQuebradas);
        command.Parameters.AddWithValue("@MontoAjusteRiosQuebradas", calculation.MontoAjusteRiosQuebradas);
        command.Parameters.AddWithValue("@PorcentajeHidrico", calculation.PorcentajeHidrico);
        command.Parameters.AddWithValue("@PorcentajeNacientes", calculation.PorcentajeNacientes);
        command.Parameters.AddWithValue("@MontoAjusteNacientes", calculation.MontoAjusteNacientes);
        command.Parameters.AddWithValue("@PorcentajePendiente", calculation.PorcentajePendiente);
        command.Parameters.AddWithValue("@MontoAjustePendiente", calculation.MontoAjustePendiente);
        command.Parameters.AddWithValue("@PorcentajeTotalAntesTope", calculation.PorcentajeAjusteTotalBruto);
        command.Parameters.AddWithValue("@PorcentajeTopeAplicado", calculation.TopePorcentajeAjuste);
        command.Parameters.AddWithValue("@PorcentajeTotalAplicado", calculation.PorcentajeAjusteAplicado);
        command.Parameters.AddWithValue("@PorcentajeRecortadoPorTope", calculation.PorcentajeRecortadoPorTope);
        command.Parameters.AddWithValue("@MontoBaseMensual", calculation.MontoBaseMensual);
        command.Parameters.AddWithValue("@MontoAjusteBrutoMensual", calculation.MontoAjusteBrutoMensual);
        command.Parameters.AddWithValue("@MontoAjusteMensual", calculation.MontoAjusteMensual);
        command.Parameters.AddWithValue("@MontoRecortadoPorTope", calculation.MontoRecortadoPorTope);
        command.Parameters.AddWithValue("@MontoFinalMensual", calculation.MontoMensualTotal);
        command.Parameters.AddWithValue("@VegetacionFinal", context.VegetacionFinal);
        command.Parameters.AddWithValue("@TieneRecursosHidricosFinal", context.TieneRecursosHidricosFinal);
        command.Parameters.AddWithValue("@CantidadNacientesFinal", context.CantidadNacientesFinal);
        command.Parameters.AddWithValue("@PendienteFinal", context.PendienteFinal);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task LimpiarCuotasAsync(SqlConnection connection, SqlTransaction tx, int idPlanPago)
    {
        using var command = new SqlCommand("DELETE FROM dbo.CuotasPago WHERE IdPlanPago = @IdPlanPago;", connection, tx);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertarCuotasAsync(SqlConnection connection, SqlTransaction tx, int idPlanPago, int anio, decimal montoMensual)
    {
        const string sql = @"
INSERT INTO dbo.CuotasPago(IdPlanPago, Mes, FechaProgramada, MontoProgramado, MontoPendiente, EstadoCuota)
VALUES(@IdPlanPago, @Mes, @FechaProgramada, @MontoProgramado, @MontoPendiente, @EstadoCuota);";

        for (var mes = 1; mes <= 12; mes++)
        {
            using var command = new SqlCommand(sql, connection, tx);
            command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
            command.Parameters.AddWithValue("@Mes", mes);
            command.Parameters.AddWithValue("@FechaProgramada", new DateTime(anio, mes, 1));
            command.Parameters.AddWithValue("@MontoProgramado", montoMensual);
            command.Parameters.AddWithValue("@MontoPendiente", montoMensual);
            command.Parameters.AddWithValue("@EstadoCuota", EstadosCuotaPago.Pendiente);
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<int> ArrastrarSaldosPendientesAsync(DateTime fechaCorte)
    {
        const string sql = @"
;WITH CuotasVencidas AS (
    SELECT c.IdCuotaPago, c.IdPlanPago, c.Mes, c.MontoPendiente
    FROM dbo.CuotasPago c
    INNER JOIN dbo.PlanesPago p ON p.IdPlanPago = c.IdPlanPago
    WHERE c.FechaProgramada < @FechaCorte
      AND c.MontoPendiente > 0
      AND c.EstadoCuota IN (@EstadoPendiente,@EstadoNotificada,@EstadoAtrasada)
      AND p.EstadoPlan = @EstadoActivo
), ProximaCuota AS (
    SELECT v.IdCuotaPago, v.MontoPendiente, nx.IdCuotaPago AS IdSiguiente
    FROM CuotasVencidas v
    OUTER APPLY (
      SELECT TOP 1 c2.IdCuotaPago
      FROM dbo.CuotasPago c2
      WHERE c2.IdPlanPago = v.IdPlanPago AND c2.Mes > v.Mes
      ORDER BY c2.Mes ASC
    ) nx
)
UPDATE cnext SET cnext.MontoPendiente = cnext.MontoPendiente + p.MontoPendiente
FROM ProximaCuota p
INNER JOIN dbo.CuotasPago cnext ON cnext.IdCuotaPago = p.IdSiguiente;

UPDATE c
SET c.EstadoCuota = @EstadoAtrasada, c.MontoPendiente = 0
FROM dbo.CuotasPago c
INNER JOIN ProximaCuota p ON p.IdCuotaPago = c.IdCuotaPago
WHERE p.IdSiguiente IS NOT NULL;

SELECT @@ROWCOUNT;";
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@FechaCorte", fechaCorte.Date);
        command.Parameters.AddWithValue("@EstadoPendiente", EstadosCuotaPago.Pendiente);
        command.Parameters.AddWithValue("@EstadoNotificada", EstadosCuotaPago.Notificada);
        command.Parameters.AddWithValue("@EstadoAtrasada", EstadosCuotaPago.Atrasada);
        command.Parameters.AddWithValue("@EstadoActivo", EstadosPlanPago.Activo);
        await connection.OpenAsync();
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

}
