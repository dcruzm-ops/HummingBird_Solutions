using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Administracion;

namespace PSA.DataAccess.DAO;

public class ConfiguracionPagoDAO
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ConfiguracionPagoDAO(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CrearConfiguracionAsync(ConfiguracionPagoAdminDTO dto)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var columnas = await ObtenerColumnasConfiguracionPagoAsync(connection);

        var insertColumns = new List<string>();
        var insertParams = new List<string>();
        using var command = new SqlCommand { Connection = connection };

        AgregarSiExiste("Version", dto.Version <= 0 ? 1 : dto.Version);
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

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result ?? 0);

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
            return new List<ConfiguracionPagoAdminDTO>();
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
            : (GetBool(reader, "Activa") ?? true);
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
            Ajustes = new List<ConfiguracionPagoAjusteDTO>()
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
}
