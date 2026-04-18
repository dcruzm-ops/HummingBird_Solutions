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
            CommandType = System.Data.CommandType.StoredProcedure
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

    public async Task<List<CuotaPlanPagoDTO>> ObtenerHistorialCuotasDuenoAsync(int idPropietario)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.SP_Pagos_ObtenerHistorialDueno", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
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

    private static PlanPagoDTO MapPlanPago(SqlDataReader reader)
    {
        return new PlanPagoDTO
        {
            IdPlanPago = reader.GetInt32(reader.GetOrdinal("IdPlanPago")),
            IdFinca = reader.GetInt32(reader.GetOrdinal("IdFinca")),
            NombreFinca = reader["NombreFinca"]?.ToString() ?? string.Empty,
            Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
            IdConfiguracionPago = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPago")),
            IdCuentaBancaria = reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria")),
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
}
