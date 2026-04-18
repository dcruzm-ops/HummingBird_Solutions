using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Administracion;

namespace PSA.DataAccess.DAO;

public class ConfiguracionPagoDAO(IDbConnectionFactory connectionFactory)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<int> CrearConfiguracionAsync(ConfiguracionPagoAdminDTO dto)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasConfiguracionPagoAsync(connection);
        var autogenerarVersion = columnas.Contains("Version", StringComparer.OrdinalIgnoreCase) && dto.Version <= 0;
        var versionCalculada = dto.Version;
        var requiereActivacionPosterior = dto.Activa && columnas.Contains("Activa", StringComparer.OrdinalIgnoreCase);

        using var tx = connection.BeginTransaction();

        for (var intento = 0; intento < 2; intento++)
        {
            if (autogenerarVersion)
            {
                versionCalculada = await ObtenerSiguienteVersionAsync(connection, tx);
            }

            var columnasInsert = new List<string>();
            var parametrosInsert = new List<string>();
            using var command = new SqlCommand { Connection = connection, Transaction = tx };

            AgregarColumnaSiExiste("Version", versionCalculada <= 0 ? 1 : versionCalculada);
            AgregarColumnaSiExiste("NombreVersion", dto.NombreVersion);
            AgregarColumnaSiExiste("PrecioBasePorHectarea", dto.PrecioBasePorHectarea);
            AgregarColumnaSiExiste("PorcentajeTopeAjuste", dto.TopePorcentajeAjuste);
            AgregarColumnaSiExiste("TopePorcentajeAjuste", dto.TopePorcentajeAjuste);
            AgregarColumnaSiExiste("FechaVigenciaDesde", dto.FechaVigenciaDesde);
            AgregarColumnaSiExiste("FechaVigenciaHasta", dto.FechaVigenciaHasta);
            AgregarColumnaSiExiste("Estado", dto.Activa && !requiereActivacionPosterior ? "Activa" : "Inactiva");
            AgregarColumnaSiExiste("Activa", requiereActivacionPosterior ? false : dto.Activa);
            AgregarColumnaSiExiste("IdAdministrador", dto.CreadoPor);
            AgregarColumnaSiExiste("CreadoPor", dto.CreadoPor);

            if (columnas.Contains("FechaCreacion", StringComparer.OrdinalIgnoreCase))
            {
                columnasInsert.Add("FechaCreacion");
                parametrosInsert.Add("SYSUTCDATETIME()");
            }

            if (columnasInsert.Count == 0)
            {
                throw new InvalidOperationException("No hay columnas válidas para insertar en ConfiguracionesPago.");
            }

            command.CommandText = $@"
INSERT INTO dbo.ConfiguracionesPago
(
    {string.Join(", ", columnasInsert)}
)
VALUES
(
    {string.Join(", ", parametrosInsert)}
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            try
            {
                var result = await command.ExecuteScalarAsync();
                var idConfiguracion = Convert.ToInt32(result ?? 0);

                await GuardarDetallesConfiguracionAsync(connection, tx, idConfiguracion, dto.Ajustes);

                if (dto.Activa)
                {
                    await AsegurarUnicaConfiguracionActivaAsync(connection, tx, idConfiguracion, columnas);
                }

                await tx.CommitAsync();
                return idConfiguracion;
            }
            catch (SqlException ex) when (autogenerarVersion && EsConflictoVersion(ex) && intento == 0)
            {
                // Reintento único recalculando versión
                continue;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            void AgregarColumnaSiExiste(string columna, object? valor)
            {
                if (!columnas.Contains(columna, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                var parametro = $"@{columna}";
                columnasInsert.Add(columna);
                parametrosInsert.Add(parametro);
                command.Parameters.AddWithValue(parametro, valor ?? DBNull.Value);
            }
        }

        await tx.RollbackAsync();
        throw new InvalidOperationException("No fue posible guardar la configuración por conflicto de versión.");
    }

    public async Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionVigenteAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasConfiguracionPagoAsync(connection);
        if (columnas.Count == 0)
        {
            return null;
        }

        var columnasSelect = string.Join(", ", columnas.OrderBy(x => x));
        var orden = ObtenerOrdenConsulta(columnas);

        using var command = new SqlCommand($"SELECT TOP 1 {columnasSelect} FROM dbo.ConfiguracionesPago ORDER BY {orden};", connection);
        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapConfiguracion(reader);
    }

    public async Task<List<ConfiguracionPagoAdminDTO>> ObtenerHistorialAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasConfiguracionPagoAsync(connection);
        if (columnas.Count == 0)
        {
            return [];
        }

        var columnasSelect = string.Join(", ", columnas.OrderBy(x => x));
        var orden = ObtenerOrdenConsulta(columnas);

        var resultado = new List<ConfiguracionPagoAdminDTO>();
        using var command = new SqlCommand($"SELECT {columnasSelect} FROM dbo.ConfiguracionesPago ORDER BY {orden};", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(MapConfiguracion(reader));
        }

        return resultado;
    }

    public async Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionDetalleAsync(int idConfiguracionPago)
    {
        if (idConfiguracionPago <= 0)
        {
            return null;
        }

        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasConfiguracionPagoAsync(connection);
        if (columnas.Count == 0)
        {
            return null;
        }

        var columnasSelect = string.Join(", ", columnas.OrderBy(x => x));
        var sql = $"SELECT {columnasSelect} FROM dbo.ConfiguracionesPago WHERE IdConfiguracionPago = @IdConfiguracionPago;";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        var configuracion = MapConfiguracion(reader);
        reader.Close();

        configuracion.Ajustes = await ObtenerAjustesConfiguracionAsync(connection, idConfiguracionPago);
        return configuracion;
    }

    private static async Task<HashSet<string>> ObtenerColumnasConfiguracionPagoAsync(SqlConnection connection)
    {
        const string sql = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'ConfiguracionesPago';";

        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = new SqlCommand(sql, connection);
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnas.Add(reader["COLUMN_NAME"]?.ToString() ?? string.Empty);
        }

        return columnas;
    }

    private static async Task<int> ObtenerSiguienteVersionAsync(SqlConnection connection, SqlTransaction tx)
    {
        const string sql = @"
SELECT ISNULL(MAX(Version), 0) + 1
FROM dbo.ConfiguracionesPago WITH (UPDLOCK, HOLDLOCK);";

        using var command = new SqlCommand(sql, connection, tx);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 1);
    }

    private static bool EsConflictoVersion(SqlException ex)
    {
        return (ex.Number == 2627 || ex.Number == 2601)
               && ex.Message.Contains("UQ_ConfiguracionesPago_Version", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<List<ConfiguracionPagoAjusteDTO>> ObtenerAjustesConfiguracionAsync(SqlConnection connection, int idConfiguracionPago)
    {
        if (!await ExisteTablaDetalleAsync(connection))
        {
            return [];
        }

        const string sql = @"
SELECT IdDetalleConfiguracion, TipoFactor, ValorFactor, PorcentajeAjuste
FROM dbo.ConfiguracionPagoDetalle
WHERE IdConfiguracionPago = @IdConfiguracionPago
ORDER BY TipoFactor, ValorFactor;";

        var resultado = new List<ConfiguracionPagoAjusteDTO>();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(new ConfiguracionPagoAjusteDTO
            {
                IdDetalleConfiguracion = reader.GetInt32(reader.GetOrdinal("IdDetalleConfiguracion")),
                TipoFactor = reader["TipoFactor"]?.ToString() ?? string.Empty,
                ValorFactor = reader["ValorFactor"]?.ToString() ?? string.Empty,
                PorcentajeAjuste = reader["PorcentajeAjuste"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["PorcentajeAjuste"])
            });
        }

        return resultado;
    }

    private static async Task GuardarDetallesConfiguracionAsync(SqlConnection connection, SqlTransaction tx, int idConfiguracionPago, List<ConfiguracionPagoAjusteDTO>? ajustes)
    {
        if (ajustes == null || ajustes.Count == 0)
        {
            return;
        }

        if (!await ExisteTablaDetalleAsync(connection, tx))
        {
            return;
        }

        const string sql = @"
INSERT INTO dbo.ConfiguracionPagoDetalle (IdConfiguracionPago, TipoFactor, ValorFactor, PorcentajeAjuste)
VALUES (@IdConfiguracionPago, @TipoFactor, @ValorFactor, @PorcentajeAjuste);";

        foreach (var ajuste in ajustes)
        {
            using var command = new SqlCommand(sql, connection, tx);
            command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
            command.Parameters.AddWithValue("@TipoFactor", ajuste.TipoFactor ?? string.Empty);
            command.Parameters.AddWithValue("@ValorFactor", ajuste.ValorFactor ?? string.Empty);
            command.Parameters.AddWithValue("@PorcentajeAjuste", ajuste.PorcentajeAjuste);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task AsegurarUnicaConfiguracionActivaAsync(SqlConnection connection, SqlTransaction tx, int idConfiguracionPago, HashSet<string> columnas)
    {
        if (columnas.Contains("Estado", StringComparer.OrdinalIgnoreCase))
        {
            const string sqlEstado = @"
UPDATE dbo.ConfiguracionesPago
SET Estado = CASE WHEN IdConfiguracionPago = @IdConfiguracionPago THEN 'Activa' ELSE 'Inactiva' END;";

            using var command = new SqlCommand(sqlEstado, connection, tx);
            command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
            await command.ExecuteNonQueryAsync();
        }

        if (columnas.Contains("Activa", StringComparer.OrdinalIgnoreCase))
        {
            const string sqlActiva = @"
UPDATE dbo.ConfiguracionesPago
SET Activa = CASE WHEN IdConfiguracionPago = @IdConfiguracionPago THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;";

            using var command = new SqlCommand(sqlActiva, connection, tx);
            command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static async Task<bool> ExisteTablaDetalleAsync(SqlConnection connection, SqlTransaction? tx = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'ConfiguracionPagoDetalle';";

        using var command = tx == null ? new SqlCommand(sql, connection) : new SqlCommand(sql, connection, tx);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0) > 0;
    }

    private static string ObtenerOrdenConsulta(HashSet<string> columnas)
    {
        if (columnas.Contains("FechaVigenciaDesde", StringComparer.OrdinalIgnoreCase)
            && columnas.Contains("IdConfiguracionPago", StringComparer.OrdinalIgnoreCase))
        {
            return "FechaVigenciaDesde DESC, IdConfiguracionPago DESC";
        }

        if (columnas.Contains("FechaCreacion", StringComparer.OrdinalIgnoreCase)
            && columnas.Contains("IdConfiguracionPago", StringComparer.OrdinalIgnoreCase))
        {
            return "FechaCreacion DESC, IdConfiguracionPago DESC";
        }

        return columnas.Contains("IdConfiguracionPago", StringComparer.OrdinalIgnoreCase)
            ? "IdConfiguracionPago DESC"
            : columnas.First() + " DESC";
    }

    private static ConfiguracionPagoAdminDTO MapConfiguracion(SqlDataReader reader)
    {
        var version = ObtenerEntero(reader, "Version") ?? 1;
        var estado = ObtenerTexto(reader, "Estado");

        return new ConfiguracionPagoAdminDTO
        {
            IdConfiguracionPago = ObtenerEntero(reader, "IdConfiguracionPago") ?? 0,
            Version = version,
            NombreVersion = ObtenerTexto(reader, "NombreVersion") ?? $"Versión {version}",
            PrecioBasePorHectarea = ObtenerDecimal(reader, "PrecioBasePorHectarea") ?? 0m,
            TopePorcentajeAjuste = ObtenerDecimal(reader, "PorcentajeTopeAjuste")
                                  ?? ObtenerDecimal(reader, "TopePorcentajeAjuste")
                                  ?? 0m,
            FechaVigenciaDesde = ObtenerFecha(reader, "FechaVigenciaDesde")
                                 ?? ObtenerFecha(reader, "FechaCreacion")
                                 ?? DateTime.UtcNow,
            FechaVigenciaHasta = ObtenerFecha(reader, "FechaVigenciaHasta"),
            Activa = estado != null
                ? string.Equals(estado, "Activa", StringComparison.OrdinalIgnoreCase)
                : (ObtenerBool(reader, "Activa") ?? true),
            CreadoPor = ObtenerEntero(reader, "IdAdministrador")
                        ?? ObtenerEntero(reader, "CreadoPor")
                        ?? 0,
            FechaCreacion = ObtenerFecha(reader, "FechaCreacion") ?? DateTime.UtcNow,
            Ajustes = []
        };
    }

    private static int? ObtenerIndiceColumna(SqlDataReader reader, string columna)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columna, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    private static string? ObtenerTexto(SqlDataReader reader, string columna)
    {
        var indice = ObtenerIndiceColumna(reader, columna);
        if (!indice.HasValue || reader.IsDBNull(indice.Value))
        {
            return null;
        }

        return reader.GetValue(indice.Value)?.ToString();
    }

    private static int? ObtenerEntero(SqlDataReader reader, string columna)
    {
        var valor = ObtenerTexto(reader, columna);
        return int.TryParse(valor, out var parsed) ? parsed : null;
    }

    private static decimal? ObtenerDecimal(SqlDataReader reader, string columna)
    {
        var indice = ObtenerIndiceColumna(reader, columna);
        if (!indice.HasValue || reader.IsDBNull(indice.Value))
        {
            return null;
        }

        var valor = reader.GetValue(indice.Value);
        return valor is decimal d ? d : decimal.TryParse(valor?.ToString(), out var parsed) ? parsed : null;
    }

    private static DateTime? ObtenerFecha(SqlDataReader reader, string columna)
    {
        var indice = ObtenerIndiceColumna(reader, columna);
        if (!indice.HasValue || reader.IsDBNull(indice.Value))
        {
            return null;
        }

        var valor = reader.GetValue(indice.Value);
        return valor is DateTime dt ? dt : DateTime.TryParse(valor?.ToString(), out var parsed) ? parsed : null;
    }

    private static bool? ObtenerBool(SqlDataReader reader, string columna)
    {
        var indice = ObtenerIndiceColumna(reader, columna);
        if (!indice.HasValue || reader.IsDBNull(indice.Value))
        {
            return null;
        }

        var valor = reader.GetValue(indice.Value);
        return valor is bool b ? b : bool.TryParse(valor?.ToString(), out var parsed) ? parsed : null;
    }
}
