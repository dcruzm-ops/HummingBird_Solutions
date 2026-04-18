using System.Data;
using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Pagos;

namespace PSA.DataAccess.DAO;

public class PlanPagoDAO(IDbConnectionFactory connectionFactory)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

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
        const string sql = @"
SELECT TOP 1
    e.IdEvaluacion,
    f.IdFinca,
    f.IdPropietario,
    f.NombreFinca,
    COALESCE(e.HectareasAjustadas, f.Hectareas) AS HectareasAprobadas,
    COALESCE(NULLIF(e.VegetacionAjustada, ''), f.Vegetacion) AS VegetacionFinal,
    COALESCE(e.RecursosHidricosAjustado, f.TieneRecursosHidricos) AS TieneRecursosHidricosFinal,
    COALESCE(f.CantidadNacientes, 0) AS CantidadNacientesFinal,
    COALESCE(NULLIF(e.PendienteAjustada, ''), f.Pendiente) AS PendienteFinal
FROM dbo.EvaluacionesTecnicas e
INNER JOIN dbo.Fincas f ON f.IdFinca = e.IdFinca
WHERE e.IdEvaluacion = @IdEvaluacion
  AND e.DecisionTecnica = 'Califica'
  AND e.EstadoEvaluacion IN ('Evaluada – Califica', 'FinalizadaCalifica');";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

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
            HectareasAprobadas = reader.GetDecimal(reader.GetOrdinal("HectareasAprobadas")),
            VegetacionFinal = reader["VegetacionFinal"]?.ToString() ?? string.Empty,
            TieneRecursosHidricosFinal = reader.GetBoolean(reader.GetOrdinal("TieneRecursosHidricosFinal")),
            CantidadNacientesFinal = reader.GetInt32(reader.GetOrdinal("CantidadNacientesFinal")),
            PendienteFinal = reader["PendienteFinal"]?.ToString() ?? string.Empty
        };
    }

    public async Task<PaymentConfigurationVersionDTO?> ObtenerConfiguracionVigenteParaAnioAsync(int anio)
    {
        const string sqlConfig = @"
SELECT TOP 1 IdConfiguracionPago, Version, PrecioBasePorHectarea, TopePorcentajeAjuste
FROM dbo.ConfiguracionesPago
WHERE Activa = 1
  AND FechaVigenciaDesde <= DATEFROMPARTS(@Anio, 1, 1)
  AND (FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= DATEFROMPARTS(@Anio, 1, 1))
ORDER BY FechaVigenciaDesde DESC, IdConfiguracionPago DESC;";

        const string sqlDetalle = @"
SELECT TipoFactor, ValorFactor, PorcentajeAjuste
FROM dbo.ConfiguracionPagoDetalle
WHERE IdConfiguracionPago = @IdConfiguracionPago;";

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

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
            idPlanPago = existingPlanId.Value;
            await ActualizarPlanAsync(connection, tx, idPlanPago, context, config, calculation, estadoInicial);
            await LimpiarCuotasAsync(connection, tx, idPlanPago);
            await UpsertDetalleCalculoAsync(connection, tx, idPlanPago, context, config, calculation);
        }
        else
        {
            idPlanPago = await InsertarPlanAsync(connection, tx, context, config, calculation, anio, estadoInicial);
            await UpsertDetalleCalculoAsync(connection, tx, idPlanPago, context, config, calculation);
        }

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
                HectareasAprobadas = context.HectareasAprobadas,
                PrecioBasePorHectarea = config.PrecioBasePorHectarea,
                MontoBaseMensual = calculation.MontoBaseMensual,
                PorcentajeVegetacion = calculation.PorcentajeVegetacion,
                PorcentajeHidrico = calculation.PorcentajeHidrico,
                PorcentajeNacientes = calculation.PorcentajeNacientes,
                PorcentajePendiente = calculation.PorcentajePendiente,
                PorcentajeTotalAntesTope = calculation.PorcentajeAjusteTotalBruto,
                PorcentajeTopeAplicado = calculation.TopePorcentajeAjuste,
                PorcentajeTotalAplicado = calculation.PorcentajeAjusteAplicado,
                MontoAjusteMensual = calculation.MontoAjusteMensual,
                MontoFinalMensual = calculation.MontoMensualTotal,
                VegetacionFinal = context.VegetacionFinal,
                TieneRecursosHidricosFinal = context.TieneRecursosHidricosFinal,
                CantidadNacientesFinal = context.CantidadNacientesFinal,
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
  AND (e.IdIngeniero = @IdIngeniero OR @IdIngeniero = 1);

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

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);
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
                PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
                MontoBaseMensual = reader.GetDecimal(reader.GetOrdinal("MontoBaseMensual")),
                PorcentajeVegetacion = reader.GetDecimal(reader.GetOrdinal("PorcentajeVegetacion")),
                PorcentajeHidrico = reader.GetDecimal(reader.GetOrdinal("PorcentajeHidrico")),
                PorcentajeNacientes = reader.GetDecimal(reader.GetOrdinal("PorcentajeNacientes")),
                PorcentajePendiente = reader.GetDecimal(reader.GetOrdinal("PorcentajePendiente")),
                PorcentajeTotalAntesTope = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAntesTope")),
                PorcentajeTopeAplicado = reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAplicado")),
                PorcentajeTotalAplicado = reader.GetDecimal(reader.GetOrdinal("PorcentajeTotalAplicado")),
                MontoAjusteMensual = reader.GetDecimal(reader.GetOrdinal("MontoAjusteMensual")),
                MontoFinalMensual = reader.GetDecimal(reader.GetOrdinal("MontoFinalMensual")),
                VegetacionFinal = reader["VegetacionFinal"]?.ToString() ?? string.Empty,
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
        PorcentajeHidrico = @PorcentajeHidrico,
        PorcentajeNacientes = @PorcentajeNacientes,
        PorcentajePendiente = @PorcentajePendiente,
        PorcentajeTotalAntesTope = @PorcentajeTotalAntesTope,
        PorcentajeTopeAplicado = @PorcentajeTopeAplicado,
        PorcentajeTotalAplicado = @PorcentajeTotalAplicado,
        MontoBaseMensual = @MontoBaseMensual,
        MontoAjusteMensual = @MontoAjusteMensual,
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
        PorcentajeVegetacion, PorcentajeHidrico, PorcentajeNacientes, PorcentajePendiente,
        PorcentajeTotalAntesTope, PorcentajeTopeAplicado, PorcentajeTotalAplicado,
        MontoBaseMensual, MontoAjusteMensual, MontoFinalMensual,
        VegetacionFinal, TieneRecursosHidricosFinal, CantidadNacientesFinal, PendienteFinal
    )
    VALUES
    (
        @IdPlanPago, @HectareasAprobadas, @PrecioBasePorHectarea,
        @PorcentajeVegetacion, @PorcentajeHidrico, @PorcentajeNacientes, @PorcentajePendiente,
        @PorcentajeTotalAntesTope, @PorcentajeTopeAplicado, @PorcentajeTotalAplicado,
        @MontoBaseMensual, @MontoAjusteMensual, @MontoFinalMensual,
        @VegetacionFinal, @TieneRecursosHidricosFinal, @CantidadNacientesFinal, @PendienteFinal
    );
END";

        using var command = new SqlCommand(sql, connection, tx);
        command.Parameters.AddWithValue("@IdPlanPago", idPlanPago);
        command.Parameters.AddWithValue("@HectareasAprobadas", context.HectareasAprobadas);
        command.Parameters.AddWithValue("@PrecioBasePorHectarea", config.PrecioBasePorHectarea);
        command.Parameters.AddWithValue("@PorcentajeVegetacion", calculation.PorcentajeVegetacion);
        command.Parameters.AddWithValue("@PorcentajeHidrico", calculation.PorcentajeHidrico);
        command.Parameters.AddWithValue("@PorcentajeNacientes", calculation.PorcentajeNacientes);
        command.Parameters.AddWithValue("@PorcentajePendiente", calculation.PorcentajePendiente);
        command.Parameters.AddWithValue("@PorcentajeTotalAntesTope", calculation.PorcentajeAjusteTotalBruto);
        command.Parameters.AddWithValue("@PorcentajeTopeAplicado", calculation.TopePorcentajeAjuste);
        command.Parameters.AddWithValue("@PorcentajeTotalAplicado", calculation.PorcentajeAjusteAplicado);
        command.Parameters.AddWithValue("@MontoBaseMensual", calculation.MontoBaseMensual);
        command.Parameters.AddWithValue("@MontoAjusteMensual", calculation.MontoAjusteMensual);
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
}
