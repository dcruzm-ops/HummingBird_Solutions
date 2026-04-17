using PSA.DataAccess.BaseDatos;
using PSA.EntidadesDTO.DTOs.Administracion;
using Microsoft.Data.SqlClient;
using System.Data;

namespace PSA.DataAccess.DAO
{
    public class ConfiguracionPagoDAO
    {
        private readonly DbContextHelper _dbContext;

        public ConfiguracionPagoDAO(DbContextHelper dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> CrearConfiguracionAsync(ConfiguracionPagoAdminDTO dto)
        {
            using var conn = _dbContext.CrearConexion();
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();
            try
            {
                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "SP_CREAR_CONFIGURACION_PAGO";

                cmd.Parameters.AddWithValue("@PrecioBasePorHectarea", dto.PrecioBasePorHectarea);
                cmd.Parameters.AddWithValue("@PorcentajeTopeAjuste", dto.PorcentajeTopeAjuste);
                cmd.Parameters.AddWithValue("@FechaVigenciaDesde", dto.FechaVigenciaDesde);
                cmd.Parameters.AddWithValue("@Observaciones", (object?)dto.Observaciones ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IdAdministrador", dto.IdAdministradorCreador ?? (object)DBNull.Value);

                var idConfiguracion = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                if (dto.Ajustes != null)
                {
                    foreach (var ajuste in dto.Ajustes)
                    {
                        var cmdDetalle = conn.CreateCommand();
                        cmdDetalle.Transaction = tx;
                        cmdDetalle.CommandType = CommandType.StoredProcedure;
                        cmdDetalle.CommandText = "SP_CREAR_CONFIGURACION_PAGO_AJUSTE";

                        cmdDetalle.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracion);
                        cmdDetalle.Parameters.AddWithValue("@TipoFactor", ajuste.TipoFactor);
                        cmdDetalle.Parameters.AddWithValue("@ValorFactor", ajuste.ValorFactor);
                        cmdDetalle.Parameters.AddWithValue("@PorcentajeAjuste", ajuste.PorcentajeAjuste);
                        cmdDetalle.Parameters.AddWithValue("@Activo", ajuste.Activo);

                        await cmdDetalle.ExecuteNonQueryAsync();
                    }
                }

                tx.Commit();
                return idConfiguracion;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionVigenteAsync()
        {
            using var conn = _dbContext.CrearConexion();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "SP_OBTENER_CONFIGURACION_PAGO_VIGENTE";

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            ConfiguracionPagoAdminDTO? configuracion = null;
            while (await reader.ReadAsync())
            {
                if (configuracion == null)
                {
                    configuracion = new ConfiguracionPagoAdminDTO
                    {
                        IdConfiguracionPago = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPago")),
                        PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
                        PorcentajeTopeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAjuste")),
                        FechaVigenciaDesde = reader.GetDateTime(reader.GetOrdinal("FechaVigenciaDesde")),
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                        Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString(reader.GetOrdinal("Observaciones")),
                        Ajustes = new List<ConfiguracionPagoAjusteDTO>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("IdConfiguracionPagoAjuste")))
                {
                    configuracion.Ajustes.Add(new ConfiguracionPagoAjusteDTO
                    {
                        IdConfiguracionPagoAjuste = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPagoAjuste")),
                        TipoFactor = reader.GetString(reader.GetOrdinal("TipoFactor")),
                        ValorFactor = reader.GetString(reader.GetOrdinal("ValorFactor")),
                        PorcentajeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeAjuste")),
                        Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
                    });
                }
            }

            return configuracion;
        }

        public async Task<List<ConfiguracionPagoAdminDTO>> ObtenerHistorialAsync()
        {
            using var conn = _dbContext.CrearConexion();
            using var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "SP_OBTENER_HISTORIAL_CONFIGURACION_PAGO";

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            var configuraciones = new Dictionary<int, ConfiguracionPagoAdminDTO>();

            while (await reader.ReadAsync())
            {
                var idConfiguracion = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPago"));
                if (!configuraciones.TryGetValue(idConfiguracion, out var configuracion))
                {
                    configuracion = new ConfiguracionPagoAdminDTO
                    {
                        IdConfiguracionPago = idConfiguracion,
                        PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
                        PorcentajeTopeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAjuste")),
                        FechaVigenciaDesde = reader.GetDateTime(reader.GetOrdinal("FechaVigenciaDesde")),
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                        Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString(reader.GetOrdinal("Observaciones")),
                        Ajustes = new List<ConfiguracionPagoAjusteDTO>()
                    };
                    configuraciones[idConfiguracion] = configuracion;
                }

                if (!reader.IsDBNull(reader.GetOrdinal("IdConfiguracionPagoAjuste")))
                {
                    configuracion.Ajustes.Add(new ConfiguracionPagoAjusteDTO
                    {
                        IdConfiguracionPagoAjuste = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPagoAjuste")),
                        TipoFactor = reader.GetString(reader.GetOrdinal("TipoFactor")),
                        ValorFactor = reader.GetString(reader.GetOrdinal("ValorFactor")),
                        PorcentajeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeAjuste")),
                        Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
                    });
                }
            }

            return configuraciones.Values.OrderByDescending(x => x.FechaVigenciaDesde).ToList();
        }
    }
}
