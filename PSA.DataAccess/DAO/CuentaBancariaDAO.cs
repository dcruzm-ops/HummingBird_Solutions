using PSA.DataAccess.BaseDatos;
using PSA.EntidadesDTO.DTOs.Administracion;
using Microsoft.Data.SqlClient;
using System.Data;

namespace PSA.DataAccess.DAO
{
    public class CuentaBancariaDAO
    {
        private readonly DbContextHelper _dbContext;

        public CuentaBancariaDAO(DbContextHelper dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CuentaBancariaPendienteDTO>> ObtenerPendientesValidacionAsync()
        {
            using var conn = _dbContext.CrearConexion();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "SP_OBTENER_CUENTAS_BANCARIAS_PENDIENTES";

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            var lista = new List<CuentaBancariaPendienteDTO>();

            while (await reader.ReadAsync())
            {
                lista.Add(new CuentaBancariaPendienteDTO
                {
                    IdCuentaBancaria = reader.GetInt32(reader.GetOrdinal("IdCuentaBancaria")),
                    IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                    NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
                    CorreoUsuario = reader.GetString(reader.GetOrdinal("CorreoUsuario")),
                    Banco = reader.GetString(reader.GetOrdinal("Banco")),
                    NumeroCuenta = reader.GetString(reader.GetOrdinal("NumeroCuenta")),
                    TipoCuenta = reader.GetString(reader.GetOrdinal("TipoCuenta")),
                    CuentaIBAN = reader.IsDBNull(reader.GetOrdinal("CuentaIBAN")) ? null : reader.GetString(reader.GetOrdinal("CuentaIBAN")),
                    FechaRegistro = reader.GetDateTime(reader.GetOrdinal("FechaRegistro"))
                });
            }

            return lista;
        }

        public async Task ValidarCuentaAsync(ValidacionCuentaBancariaDTO dto)
        {
            using var conn = _dbContext.CrearConexion();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "SP_VALIDAR_CUENTA_BANCARIA";

            cmd.Parameters.AddWithValue("@IdCuentaBancaria", dto.IdCuentaBancaria);
            cmd.Parameters.AddWithValue("@IdAdministrador", dto.IdAdministrador);
            cmd.Parameters.AddWithValue("@EstadoValidacion", dto.EstadoValidacion);
            cmd.Parameters.AddWithValue("@Observaciones", (object?)dto.Observaciones ?? DBNull.Value);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
