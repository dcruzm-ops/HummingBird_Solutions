using Microsoft.Data.SqlClient;
using System.Data;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Administracion;

namespace PSA.DataAccess.DAO;

public class CuentaBancariaDAO
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CuentaBancariaDAO(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<CuentaBancariaPendienteDTO>> ObtenerPendientesValidacionAsync()
    {
        var resultado = new List<CuentaBancariaPendienteDTO>();

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasCuentaBancariaAsync(connection);
        var expresionEstadoCuenta = columnas.Contains("Activa", StringComparer.OrdinalIgnoreCase)
            ? "CASE WHEN cb.Activa = 1 THEN 'Activa' ELSE 'Inactiva' END AS EstadoCuenta"
            : columnas.Contains("Estado", StringComparer.OrdinalIgnoreCase)
                ? "cb.Estado AS EstadoCuenta"
                : "'Desconocida' AS EstadoCuenta";
        var expresionFecha = columnas.Contains("FechaRegistro", StringComparer.OrdinalIgnoreCase)
            ? "cb.FechaRegistro AS FechaReferencia"
            : columnas.Contains("FechaCreacion", StringComparer.OrdinalIgnoreCase)
                ? "cb.FechaCreacion AS FechaReferencia"
                : "SYSUTCDATETIME() AS FechaReferencia";
        var ordenFecha = columnas.Contains("FechaRegistro", StringComparer.OrdinalIgnoreCase)
            ? "cb.FechaRegistro DESC"
            : columnas.Contains("FechaCreacion", StringComparer.OrdinalIgnoreCase)
                ? "cb.FechaCreacion DESC"
                : "cb.IdCuentaBancaria DESC";
        var sql = $@"
SELECT
    cb.IdCuentaBancaria,
    cb.IdUsuario,
    u.NombreCompleto AS NombreUsuario,
    u.Email AS EmailUsuario,
    cb.Banco,
    cb.NumeroCuenta,
    cb.TipoCuenta,
    cb.Titular,
    cb.EstadoValidacion,
    cb.ObservacionesValidacion,
    {expresionEstadoCuenta},
    {expresionFecha}
FROM dbo.CuentasBancarias cb
INNER JOIN dbo.Usuarios u ON u.IdUsuario = cb.IdUsuario
WHERE cb.EstadoValidacion = 'Pendiente'
ORDER BY {ordenFecha};";

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(new CuentaBancariaPendienteDTO
            {
                IdCuentaBancaria = reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria")),
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreUsuario = reader["NombreUsuario"]?.ToString() ?? string.Empty,
                EmailUsuario = reader["EmailUsuario"]?.ToString() ?? string.Empty,
                Banco = reader["Banco"]?.ToString() ?? string.Empty,
                NumeroCuenta = reader["NumeroCuenta"]?.ToString() ?? string.Empty,
                TipoCuenta = reader["TipoCuenta"]?.ToString() ?? string.Empty,
                Titular = reader["Titular"]?.ToString() ?? string.Empty,
                EstadoValidacion = reader["EstadoValidacion"]?.ToString() ?? string.Empty,
                ObservacionesValidacion = reader["ObservacionesValidacion"] == DBNull.Value ? null : reader["ObservacionesValidacion"]?.ToString(),
                Activa = string.Equals(reader["EstadoCuenta"]?.ToString(), "Activa", StringComparison.OrdinalIgnoreCase),
                FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaReferencia"))
            });
        }

        return resultado;
    }


    public async Task<List<PSA.EntidadesDTO.DTOs.Pagos.CuentaBancariaDuenoDTO>> ObtenerCuentasBancariasDuenoAsync(int idUsuario)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        using var command = new SqlCommand("dbo.SP_Pagos_ObtenerCuentasBancariasDueno", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);

        using var reader = await command.ExecuteReaderAsync();
        var resultado = new List<PSA.EntidadesDTO.DTOs.Pagos.CuentaBancariaDuenoDTO>();
        while (await reader.ReadAsync())
        {
            resultado.Add(new PSA.EntidadesDTO.DTOs.Pagos.CuentaBancariaDuenoDTO
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

    public async Task<int> RegistrarCuentaBancariaDuenoAsync(PSA.EntidadesDTO.DTOs.Pagos.RegistrarCuentaBancariaDTO dto)
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
        catch (SqlException ex) when (ex.Number == 57011)
        {
            throw new InvalidOperationException("Ya existe una cuenta con ese número en estado pendiente o validada.", ex);
        }
        catch (SqlException ex) when (ex.Message.Contains("CK_Cuentas_TipoCuenta", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El tipo de cuenta no es válido. Use: Ahorro, Corriente, IBAN, SINPE u Otra.", ex);
        }
    }

    public async Task ValidarCuentaAsync(ValidacionCuentaBancariaDTO dto)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasCuentaBancariaAsync(connection);
        var asignaciones = new List<string> { "EstadoValidacion = @EstadoValidacion" };
        var sql = string.Empty;
        using var command = new SqlCommand { Connection = connection };

        command.Parameters.AddWithValue("@IdCuentaBancaria", dto.IdCuentaBancaria);
        command.Parameters.AddWithValue("@EstadoValidacion", dto.Aprobada ? "Validada" : "Rechazada");

        if (columnas.Contains("ObservacionesValidacion", StringComparer.OrdinalIgnoreCase))
        {
            asignaciones.Add("ObservacionesValidacion = @Observaciones");
            command.Parameters.AddWithValue("@Observaciones", (object?)dto.Observaciones ?? DBNull.Value);
        }

        if (columnas.Contains("ValidadoPor", StringComparer.OrdinalIgnoreCase)
            && dto.IdAdministrador > 0
            && await UsuarioExisteAsync(connection, dto.IdAdministrador))
        {
            asignaciones.Add("ValidadoPor = @ValidadoPor");
            command.Parameters.AddWithValue("@ValidadoPor", dto.IdAdministrador);
        }

        if (columnas.Contains("FechaValidacion", StringComparer.OrdinalIgnoreCase))
        {
            asignaciones.Add("FechaValidacion = SYSUTCDATETIME()");
        }

        if (columnas.Contains("FechaActualizacion", StringComparer.OrdinalIgnoreCase))
        {
            asignaciones.Add("FechaActualizacion = SYSUTCDATETIME()");
        }

        if (dto.Aprobada && columnas.Contains("Activa", StringComparer.OrdinalIgnoreCase))
        {
            asignaciones.Add("Activa = 1");
        }

        sql = $@"
UPDATE dbo.CuentasBancarias
SET {string.Join(", ", asignaciones)}
WHERE IdCuentaBancaria = @IdCuentaBancaria;";
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<bool> UsuarioExisteAsync(SqlConnection connection, int idUsuario)
    {
        const string sql = @"
SELECT 1
FROM dbo.Usuarios
WHERE IdUsuario = @IdUsuario;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdUsuario", idUsuario);
        var existe = await command.ExecuteScalarAsync();
        return existe != null && existe != DBNull.Value;
    }

    private static async Task<HashSet<string>> ObtenerColumnasCuentaBancariaAsync(SqlConnection connection)
    {
        const string sql = @"
SELECT c.COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = 'dbo'
  AND c.TABLE_NAME = 'CuentasBancarias';";

        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
        {
            columnas.Add(reader["COLUMN_NAME"]?.ToString() ?? string.Empty);
        }

        return columnas;
    }
}
