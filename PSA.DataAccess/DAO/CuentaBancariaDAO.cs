using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Administracion;

namespace PSA.DataAccess.DAO
{
    public class CuentaBancariaDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CuentaBancariaDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<CuentaBancariaPendienteDTO>> ObtenerPendientesAsync()
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
    cb.Activa,
    cb.FechaRegistro
FROM CuentasBancarias cb
INNER JOIN Usuarios u ON u.IdUsuario = cb.IdUsuario
WHERE cb.EstadoValidacion = 'Pendiente'
ORDER BY cb.FechaRegistro ASC;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            var cuentas = new List<CuentaBancariaPendienteDTO>();
            while (await reader.ReadAsync())
            {
                cuentas.Add(new CuentaBancariaPendienteDTO
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
                    Activa = reader.GetBoolean(reader.GetOrdinal("Activa")),
                    FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaRegistro"))
                });
            }
            return cuentas;
        }

        public async Task<bool> ValidarCuentaAsync(ValidacionCuentaBancariaDTO dto)
        {
            const string sql = @"
UPDATE CuentasBancarias
SET EstadoValidacion = @EstadoValidacion,
    ValidadoPor = @ValidadoPor,
    FechaValidacion = SYSDATETIME(),
    ObservacionesValidacion = @ObservacionesValidacion,
    Activa = @Activa
WHERE IdCuentaBancaria = @IdCuentaBancaria
  AND EstadoValidacion = 'Pendiente';";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@EstadoValidacion", dto.Aprobada ? "Validada" : "Rechazada");
            command.Parameters.AddWithValue("@ValidadoPor", dto.IdAdministrador);
            command.Parameters.AddWithValue("@ObservacionesValidacion", (object?)dto.Observaciones?.Trim() ?? DBNull.Value);
            command.Parameters.AddWithValue("@Activa", dto.Aprobada);
            command.Parameters.AddWithValue("@IdCuentaBancaria", dto.IdCuentaBancaria);
            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
