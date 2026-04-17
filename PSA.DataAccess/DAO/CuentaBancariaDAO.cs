using Microsoft.Data.SqlClient;
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
        const string sql = @"
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
    cb.Estado AS EstadoCuenta,
    cb.FechaCreacion
FROM dbo.CuentasBancarias cb
INNER JOIN dbo.Usuarios u ON u.IdUsuario = cb.IdUsuario
ORDER BY cb.FechaCreacion DESC;";

        var resultado = new List<CuentaBancariaPendienteDTO>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
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
                FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaCreacion"))
            });
        }

        return resultado;
    }

    public async Task ValidarCuentaAsync(ValidacionCuentaBancariaDTO dto)
    {
        const string sql = @"
UPDATE dbo.CuentasBancarias
SET
    EstadoValidacion = @EstadoValidacion,
    ObservacionesValidacion = @Observaciones,
    FechaActualizacion = SYSUTCDATETIME()
WHERE IdCuentaBancaria = @IdCuentaBancaria;";

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@IdCuentaBancaria", dto.IdCuentaBancaria);
        command.Parameters.AddWithValue("@EstadoValidacion", dto.Aprobada ? "Aprobada" : "Rechazada");
        command.Parameters.AddWithValue("@Observaciones", (object?)dto.Observaciones ?? DBNull.Value);

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}
