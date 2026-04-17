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
        var versionCalculada = dto.Version;
        var autoGenerarVersion = versionCalculada <= 0 && columnas.Contains("Version", StringComparer.OrdinalIgnoreCase);
        using var tx = connection.BeginTransaction();

        for (var intento = 0; intento < 2; intento++)
        {
            if (autoGenerarVersion)
            {
                versionCalculada = await ObtenerSiguienteVersionAsync(connection, tx);
            }

            var insertColumns = new List<string>();
            var insertParams = new List<string>();
            using var command = new SqlCommand { Connection = connection, Transaction = tx };

            AgregarSiExiste("Version", versionCalculada <= 0 ? 1 : versionCalculada);
            AgregarSiExiste("NombreVersion", dto.NombreVersion);
            AgregarSiExiste("PrecioBasePorHectarea", dto.PrecioBasePorHectarea);
            AgregarSiExiste("PorcentajeTopeAjuste", dto.TopePorcentajeAjuste);
            AgregarSiExiste("TopePorcentajeAjuste", dto.TopePorcentajeAjuste);
            AgregarSiExiste("FechaVigenciaDesde", dto.FechaVigenciaDesde);
            AgregarSiExiste("FechaVigenciaHasta", dto.FechaVigenciaHasta);
            AgregarSiExiste("Estado", dto.Activa ? "Activa" : "Inactiva");
            AgregarSiExiste("IdAdministrador", dto.CreadoPor);
            AgregarSiExiste("CreadoPor", dto.CreadoPor);

            if (columnas.Contains("FechaCreacion", StringComparer.OrdinalIgnoreCase))
            {
                insertColumns.Add("FechaCreacion");
                insertParams.Add("SYSUTCDATETIME()");
            }

            if (insertColumns.Count == 0)
            {
                throw new InvalidOperationException("No hay columnas válidas para insertar en ConfiguracionesPago.");
            }

            var sql = $@"
INSERT INTO dbo.ConfiguracionesPago
(
    {string.Join(", ", insertColumns)}
)
VALUES
(
    {string.Join(", ", insertParams)}
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            command.CommandText = sql;

            try
            {
                var result = await command.ExecuteScalarAsync();
                var idConfiguracion = Convert.ToInt32(result ?? 0);
                await GuardarDetallesConfiguracionAsync(connection, tx, idConfiguracion, dto.Ajustes);
                if (dto.Activa)
                {
                    await EnsureSingleActiveConfigurationAsync(connection, tx, idConfiguracion, columnas);
                }

                await tx.CommitAsync();
                return idConfiguracion;
            }
            catch (SqlException ex) when (autoGenerarVersion && IsDuplicateVersionConstraint(ex) && intento == 0)
            {
                continue;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            void AgregarSiExiste(string columna, object? valor)
            {
                if (!columnas.Contains(columna, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }

                var parametro = $"@{columna}";
                insertColumns.Add(columna);
                insertParams.Add(parametro);
                command.Parameters.AddWithValue(parametro, valor ?? DBNull.Value);
            }
        }

        await tx.RollbackAsync();
        throw new InvalidOperationException("No fue posible guardar la configuración de pago por conflicto de versión.");
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

        var sql = $"SELECT TOP 1 {columnasSelect} FROM dbo.ConfiguracionesPago ORDER BY {orden};";

        using var command = new SqlCommand(sql, connection);
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

        var sql = $"SELECT {columnasSelect} FROM dbo.ConfiguracionesPago ORDER BY {orden};";

        var resultado = new List<ConfiguracionPagoAdminDTO>();
        using var command = new SqlCommand(sql, connection);
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

        if (columnas.Contains("IdConfiguracionPago", StringComparer.OrdinalIgnoreCase))
        {
            return "IdConfiguracionPago DESC";
        }

        return columnas.First() + " DESC";
    }

    private static ConfiguracionPagoAdminDTO MapConfiguracion(SqlDataReader reader)
    {
        var id = GetInt(reader, "IdConfiguracionPago") ?? 0;
        var version = GetInt(reader, "Version") ?? 1;
        var nombreVersion = GetString(reader, "NombreVersion") ?? $"Versión {version}";
        var precioBase = GetDecimal(reader, "PrecioBasePorHectarea") ?? 0m;
        var tope = GetDecimal(reader, "PorcentajeTopeAjuste")
                   ?? GetDecimal(reader, "TopePorcentajeAjuste")
                   ?? 0m;
        var fechaDesde = GetDateTime(reader, "FechaVigenciaDesde")
                         ?? GetDateTime(reader, "FechaCreacion")
                         ?? DateTime.UtcNow;
        var fechaHasta = GetDateTime(reader, "FechaVigenciaHasta");
        var estado = GetString(reader, "Estado");
        var activa = estado != null
            ? string.Equals(estado, "Activa", StringComparison.OrdinalIgnoreCase)
            : (GetBoolNullableValue(reader, "Activa") ?? true);
        var creadoPor = GetInt(reader, "IdAdministrador")
                        ?? GetInt(reader, "CreadoPor")
                        ?? 0;
        var fechaCreacion = GetDateTime(reader, "FechaCreacion") ?? fechaDesde;

        return new ConfiguracionPagoAdminDTO
        {
            IdConfiguracionPago = id,
            Version = version,
            NombreVersion = nombreVersion,
            PrecioBasePorHectarea = precioBase,
            TopePorcentajeAjuste = tope,
            FechaVigenciaDesde = fechaDesde,
            FechaVigenciaHasta = fechaHasta,
            Activa = activa,
            CreadoPor = creadoPor,
            FechaCreacion = fechaCreacion,
            Ajustes = []
        };
    }

    private static async Task<HashSet<string>> ObtenerColumnasConfiguracionPagoAsync(SqlConnection connection)
    {
        const string sql = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'ConfiguracionesPago';";

        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var cmd = new SqlCommand(sql, connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnas.Add(reader["COLUMN_NAME"]?.ToString() ?? string.Empty);
        }

        return columnas;
    }

    private static int? GetOrdinal(SqlDataReader reader, string columna)
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

    private static string? GetString(SqlDataReader reader, string columna)
    {
        var ordinal = GetOrdinal(reader, columna);
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        return reader.GetValue(ordinal.Value)?.ToString();
    }

    private static int? GetInt(SqlDataReader reader, string columna)
    {
        var valor = GetString(reader, columna);
        return int.TryParse(valor, out var parsed) ? parsed : null;
    }

    private static decimal? GetDecimal(SqlDataReader reader, string columna)
    {
        var ordinal = GetOrdinal(reader, columna);
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        var raw = reader.GetValue(ordinal.Value);
        return raw is decimal d ? d : decimal.TryParse(raw?.ToString(), out var parsed) ? parsed : null;
    }

    private static DateTime? GetDateTime(SqlDataReader reader, string columna)
    {
        var ordinal = GetOrdinal(reader, columna);
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        var raw = reader.GetValue(ordinal.Value);
        return raw is DateTime dt ? dt : DateTime.TryParse(raw?.ToString(), out var parsed) ? parsed : null;
    }

    private static bool? GetBoolNullableValue(SqlDataReader reader, string columna)
    {
        var ordinal = GetOrdinal(reader, columna);
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        var raw = reader.GetValue(ordinal.Value);
        return raw is bool b ? b : bool.TryParse(raw?.ToString(), out var parsed) ? parsed : null;
    }

    private static bool IsDuplicateVersionConstraint(SqlException ex)
    {
        return (ex.Number == 2627 || ex.Number == 2601)
               && ex.Message.Contains("UQ_ConfiguracionesPago_Version", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> ObtenerSiguienteVersionAsync(SqlConnection connection, SqlTransaction transaction)
    {
        const string sql = @"
SELECT ISNULL(MAX(Version), 0) + 1
FROM dbo.ConfiguracionesPago WITH (UPDLOCK, HOLDLOCK);";
        using var command = new SqlCommand(sql, connection, transaction);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 1);
    }

    private static async Task<List<ConfiguracionPagoAjusteDTO>> ObtenerAjustesConfiguracionAsync(SqlConnection connection, int idConfiguracionPago)
    {
        if (!await DetailTableExistsAsync(connection))
        {
            return [];
        }

        const string sql = @"
SELECT
    d.IdDetalleConfiguracion,
    d.TipoFactor,
    d.ValorFactor,
    d.PorcentajeAjuste
FROM dbo.ConfiguracionPagoDetalle d
WHERE d.IdConfiguracionPago = @IdConfiguracionPago
ORDER BY d.TipoFactor, d.ValorFactor;";

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

    private static async Task GuardarDetallesConfiguracionAsync(
        SqlConnection connection,
        SqlTransaction tx,
        int idConfiguracionPago,
        List<ConfiguracionPagoAjusteDTO>? ajustes)
    {
        if (ajustes == null || ajustes.Count == 0)
        {
            return;
        }

        if (!await DetailTableExistsAsync(connection, tx))
        {
            return;
        }

        const string sql = @"
INSERT INTO dbo.ConfiguracionPagoDetalle
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

        foreach (var ajuste in ajustes)
        {
            using var command = new SqlCommand(sql, connection, tx);
            command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
            command.Parameters.AddWithValue("@TipoFactor", ajuste.TipoFactor ?? string.Empty);
            command.Parameters.AddWithValue("@ValorFactor", ajuste.ValorFactor ?? string.Empty);
            command.Parameters.AddWithValue("@PorcentajeAjuste", ajuste.PorcentajeAjuste);
            await command.ExecuteNonQueryAsync();
        }

        if (!await ExisteTablaDetalleAsync(connection, tx))
        {
            return;
        }

        const string sql = @"
INSERT INTO dbo.ConfiguracionPagoDetalle
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

        foreach (var ajuste in ajustes)
        {
            using var command = new SqlCommand(sql, connection, tx);
            command.Parameters.AddWithValue("@IdConfiguracionPago", idConfiguracionPago);
            command.Parameters.AddWithValue("@TipoFactor", ajuste.TipoFactor ?? string.Empty);
            command.Parameters.AddWithValue("@ValorFactor", ajuste.ValorFactor ?? string.Empty);
            command.Parameters.AddWithValue("@PorcentajeAjuste", ajuste.PorcentajeAjuste);
            await command.ExecuteNonQueryAsync();
        }

        const string sql = @"
INSERT INTO dbo.ConfiguracionPagoDetalle
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

    private static async Task DesactivarOtrasConfiguracionesAsync(
        SqlConnection connection,
        SqlTransaction tx,
        int idConfiguracionPago,
        HashSet<string> columnas)
    {
        if (columnas.Contains("Estado", StringComparer.OrdinalIgnoreCase))
        {
            const string sqlEstado = @"
UPDATE dbo.ConfiguracionesPago
SET Estado = CASE WHEN IdConfiguracionPago = @IdConfiguracionPago THEN 'Activa' ELSE 'Inactiva' END
WHERE Estado IS NOT NULL;";
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

        var raw = reader.GetValue(ordinal.Value);
        return raw is DateTime dt ? dt : DateTime.TryParse(raw?.ToString(), out var parsed) ? parsed : null;
    }

    private static bool? GetBool(SqlDataReader reader, string columna)
    {
        var ordinal = GetOrdinal(reader, columna);
        if (!ordinal.HasValue || reader.IsDBNull(ordinal.Value))
        {
            return null;
        }

        var raw = reader.GetValue(ordinal.Value);
        return raw is bool b ? b : bool.TryParse(raw?.ToString(), out var parsed) ? parsed : null;
    }

    private static bool EsErrorVersionDuplicada(SqlException ex)
    {
        return (ex.Number == 2627 || ex.Number == 2601)
               && ex.Message.Contains("UQ_ConfiguracionesPago_Version", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> ObtenerSiguienteVersionAsync(SqlConnection connection)
    {
        const string sql = @"
SELECT ISNULL(MAX(Version), 0) + 1
FROM dbo.ConfiguracionesPago;";
        using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 1);
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

    private static async Task DesactivarOtrasConfiguracionesAsync(
        SqlConnection connection,
        SqlTransaction tx,
        int idConfiguracionPago,
        HashSet<string> columnas)
    {
        if (columnas.Contains("Estado", StringComparer.OrdinalIgnoreCase))
        {
            const string sqlEstado = @"
UPDATE dbo.ConfiguracionesPago
SET Estado = CASE WHEN IdConfiguracionPago = @IdConfiguracionPago THEN 'Activa' ELSE 'Inactiva' END
WHERE Estado IS NOT NULL;";
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

    private static async Task EnsureSingleActiveConfigurationAsync(
        SqlConnection connection,
        SqlTransaction tx,
        int idConfiguracionPago,
        HashSet<string> columnas)
    {
        if (columnas.Contains("Estado", StringComparer.OrdinalIgnoreCase))
        {
            const string sqlEstado = @"
UPDATE dbo.ConfiguracionesPago
SET Estado = CASE WHEN IdConfiguracionPago = @IdConfiguracionPago THEN 'Activa' ELSE 'Inactiva' END
WHERE Estado IS NOT NULL;";
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

    private static async Task<bool> DetailTableExistsAsync(SqlConnection connection, SqlTransaction? tx = null)
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
}
