using Microsoft.Data.SqlClient;
using PSA.EntidadesDTO.DTOs.Administracion;

namespace PSA.DataAccess.DAO
{
    public class ConfiguracionPagoDAO
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ConfiguracionPagoDAO(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<List<ConfiguracionPagoAdminDTO>> ObtenerHistorialAsync()
        {
            const string sql = @"
SELECT
    cp.IdConfiguracionPago,
    cp.Version,
    cp.NombreVersion,
    cp.PrecioBasePorHectarea,
    cp.TopePorcentajeAjuste,
    cp.FechaVigenciaDesde,
    cp.FechaVigenciaHasta,
    cp.Activa,
    cp.CreadoPor,
    cp.FechaCreacion,
    d.IdDetalleConfiguracion,
    d.TipoFactor,
    d.ValorFactor,
    d.PorcentajeAjuste
FROM ConfiguracionesPago cp
LEFT JOIN ConfiguracionPagoDetalle d ON d.IdConfiguracionPago = cp.IdConfiguracionPago
ORDER BY cp.Version DESC, d.TipoFactor, d.ValorFactor;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            return await MapearConfiguracionesAsync(reader);
        }

        public async Task<ConfiguracionPagoAdminDTO?> ObtenerVigenteAsync()
        {
            const string sql = @"
SELECT
    cp.IdConfiguracionPago,
    cp.Version,
    cp.NombreVersion,
    cp.PrecioBasePorHectarea,
    cp.TopePorcentajeAjuste,
    cp.FechaVigenciaDesde,
    cp.FechaVigenciaHasta,
    cp.Activa,
    cp.CreadoPor,
    cp.FechaCreacion,
    d.IdDetalleConfiguracion,
    d.TipoFactor,
    d.ValorFactor,
    d.PorcentajeAjuste
FROM ConfiguracionesPago cp
LEFT JOIN ConfiguracionPagoDetalle d ON d.IdConfiguracionPago = cp.IdConfiguracionPago
WHERE cp.Activa = 1
ORDER BY cp.Version DESC, d.TipoFactor, d.ValorFactor;";

            using var connection = _connectionFactory.CreateConnection();
            using var command = new SqlCommand(sql, connection);
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            return (await MapearConfiguracionesAsync(reader)).FirstOrDefault();
        }

        public async Task<int> CrearConfiguracionAsync(ConfiguracionPagoAdminDTO dto)
        {
            using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var version = dto.Version;
                if (version <= 0)
                {
                    const string sqlVersion = "SELECT ISNULL(MAX(Version), 0) + 1 FROM ConfiguracionesPago;";
                    using var commandVersion = new SqlCommand(sqlVersion, connection, transaction);
                    var versionDb = await commandVersion.ExecuteScalarAsync();
                    version = Convert.ToInt32(versionDb ?? 1);
                }

                const string sqlDesactivar = @"
UPDATE ConfiguracionesPago
SET Activa = 0,
    FechaVigenciaHasta = CASE
        WHEN FechaVigenciaHasta IS NULL OR FechaVigenciaHasta >= DATEADD(DAY, -1, @FechaVigenciaDesde)
            THEN DATEADD(DAY, -1, @FechaVigenciaDesde)
        ELSE FechaVigenciaHasta
    END
WHERE Activa = 1;";

                using (var commandDesactivar = new SqlCommand(sqlDesactivar, connection, transaction))
                {
                    commandDesactivar.Parameters.AddWithValue("@FechaVigenciaDesde", dto.FechaVigenciaDesde.Date);
                    await commandDesactivar.ExecuteNonQueryAsync();
                }

                const string sqlInsert = @"
INSERT INTO ConfiguracionesPago
(
    Version,
    NombreVersion,
    PrecioBasePorHectarea,
    TopePorcentajeAjuste,
    FechaVigenciaDesde,
    FechaVigenciaHasta,
    Activa,
    CreadoPor
)
VALUES
(
    @Version,
    @NombreVersion,
    @PrecioBasePorHectarea,
    @TopePorcentajeAjuste,
    @FechaVigenciaDesde,
    @FechaVigenciaHasta,
    1,
    @CreadoPor
);

SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int idConfiguracion;
                using (var commandInsert = new SqlCommand(sqlInsert, connection, transaction))
                {
                    commandInsert.Parameters.AddWithValue("@Version", version);
                    commandInsert.Parameters.AddWithValue("@NombreVersion", dto.NombreVersion.Trim());
                    commandInsert.Parameters.AddWithValue("@PrecioBasePorHectarea", dto.PrecioBasePorHectarea);
                    commandInsert.Parameters.AddWithValue("@TopePorcentajeAjuste", dto.TopePorcentajeAjuste);
                    commandInsert.Parameters.AddWithValue("@FechaVigenciaDesde", dto.FechaVigenciaDesde.Date);
                    commandInsert.Parameters.AddWithValue("@FechaVigenciaHasta", (object?)dto.FechaVigenciaHasta?.Date ?? DBNull.Value);
                    commandInsert.Parameters.AddWithValue("@CreadoPor", dto.CreadoPor);
                    var resultado = await commandInsert.ExecuteScalarAsync();
                    idConfiguracion = Convert.ToInt32(resultado ?? 0);
                }

                if (dto.Ajustes != null && dto.Ajustes.Count > 0)
                {
                    const string sqlInsertDetalle = @"
INSERT INTO ConfiguracionPagoDetalle
(
    IdConfiguracionPago,
    TipoFactor,
    ValorFactor,
    PorcentajeAjuste
)
VALUES
(
    @IdConfiguracionPago,
    @TipoFactor,
    @ValorFactor,
    @PorcentajeAjuste
);";

                    foreach (var ajuste in dto.Ajustes.Where(x => !string.IsNullOrWhiteSpace(x.TipoFactor) && !string.IsNullOrWhiteSpace(x.ValorFactor)))
                    {
                        using var commandDetalle = new SqlCommand(sqlInsertDetalle, connection, transaction);
                        commandDetalle.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracion);
                        commandDetalle.Parameters.AddWithValue("@TipoFactor", ajuste.TipoFactor.Trim());
                        commandDetalle.Parameters.AddWithValue("@ValorFactor", ajuste.ValorFactor.Trim());
                        commandDetalle.Parameters.AddWithValue("@PorcentajeAjuste", ajuste.PorcentajeAjuste);
                        await commandDetalle.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return idConfiguracion;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static async Task<List<ConfiguracionPagoAdminDTO>> MapearConfiguracionesAsync(SqlDataReader reader)
        {
            var configuraciones = new Dictionary<int, ConfiguracionPagoAdminDTO>();
            while (await reader.ReadAsync())
            {
                var idConfiguracion = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPago"));
                if (!configuraciones.TryGetValue(idConfiguracion, out var configuracion))
                {
                    configuracion = new ConfiguracionPagoAdminDTO
                    {
                        IdConfiguracionPago = idConfiguracion,
                        Version = reader.GetInt32(reader.GetOrdinal("Version")),
                        NombreVersion = reader["NombreVersion"]?.ToString() ?? string.Empty,
                        PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
                        TopePorcentajeAjuste = reader.GetDecimal(reader.GetOrdinal("TopePorcentajeAjuste")),
                        FechaVigenciaDesde = reader.GetDateTime(reader.GetOrdinal("FechaVigenciaDesde")),
                        FechaVigenciaHasta = reader["FechaVigenciaHasta"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaVigenciaHasta")),
                        Activa = reader.GetBoolean(reader.GetOrdinal("Activa")),
                        CreadoPor = reader.GetInt32(reader.GetOrdinal("CreadoPor")),
                        FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion"))
                    };
                    configuraciones[idConfiguracion] = configuracion;
                }

                if (reader["IdDetalleConfiguracion"] != DBNull.Value)
                {
                    configuracion.Ajustes.Add(new ConfiguracionPagoAjusteDTO
                    {
                        IdDetalleConfiguracion = reader.GetInt32(reader.GetOrdinal("IdDetalleConfiguracion")),
                        TipoFactor = reader["TipoFactor"]?.ToString() ?? string.Empty,
                        ValorFactor = reader["ValorFactor"]?.ToString() ?? string.Empty,
                        PorcentajeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeAjuste"))
                    });
                }
            }

            return configuraciones.Values.OrderByDescending(x => x.Version).ToList();
        }
    }
}
