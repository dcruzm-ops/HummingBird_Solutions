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
        const string sql = @"
INSERT INTO dbo.ConfiguracionesPago
(
    Version,
    NombreVersion,
    PrecioBasePorHectarea,
    PorcentajeTopeAjuste,
    FechaVigenciaDesde,
    FechaVigenciaHasta,
    Estado,
    IdAdministrador,
    FechaCreacion
)
VALUES
(
    @Version,
    @NombreVersion,
    @PrecioBasePorHectarea,
    @PorcentajeTopeAjuste,
    @FechaVigenciaDesde,
    @FechaVigenciaHasta,
    CASE WHEN @Activa = 1 THEN 'Activa' ELSE 'Inactiva' END,
    @IdAdministrador,
    SYSUTCDATETIME()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Version", dto.Version <= 0 ? 1 : dto.Version);
        command.Parameters.AddWithValue("@NombreVersion", dto.NombreVersion);
        command.Parameters.AddWithValue("@PrecioBasePorHectarea", dto.PrecioBasePorHectarea);
        command.Parameters.AddWithValue("@PorcentajeTopeAjuste", dto.TopePorcentajeAjuste);
        command.Parameters.AddWithValue("@FechaVigenciaDesde", dto.FechaVigenciaDesde);
        command.Parameters.AddWithValue("@FechaVigenciaHasta", (object?)dto.FechaVigenciaHasta ?? DBNull.Value);
        command.Parameters.AddWithValue("@Activa", dto.Activa);
        command.Parameters.AddWithValue("@IdAdministrador", dto.CreadoPor);

        await connection.OpenAsync();
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<ConfiguracionPagoAdminDTO?> ObtenerConfiguracionVigenteAsync()
    {
        const string sql = @"
SELECT TOP 1
    IdConfiguracionPago,
    Version,
    NombreVersion,
    PrecioBasePorHectarea,
    PorcentajeTopeAjuste,
    FechaVigenciaDesde,
    FechaVigenciaHasta,
    Estado,
    IdAdministrador,
    FechaCreacion
FROM dbo.ConfiguracionesPago
WHERE Estado = 'Activa'
ORDER BY FechaVigenciaDesde DESC, IdConfiguracionPago DESC;";

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapConfiguracion(reader);
    }

    public async Task<List<ConfiguracionPagoAdminDTO>> ObtenerHistorialAsync()
    {
        const string sql = @"
SELECT
    IdConfiguracionPago,
    Version,
    NombreVersion,
    PrecioBasePorHectarea,
    PorcentajeTopeAjuste,
    FechaVigenciaDesde,
    FechaVigenciaHasta,
    Estado,
    IdAdministrador,
    FechaCreacion
FROM dbo.ConfiguracionesPago
ORDER BY FechaVigenciaDesde DESC, IdConfiguracionPago DESC;";

        var resultado = new List<ConfiguracionPagoAdminDTO>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(MapConfiguracion(reader));
        }

        return resultado;
    }

    private static ConfiguracionPagoAdminDTO MapConfiguracion(SqlDataReader reader)
    {
        var estado = reader["Estado"]?.ToString() ?? string.Empty;

        return new ConfiguracionPagoAdminDTO
        {
            IdConfiguracionPago = reader.GetInt32(reader.GetOrdinal("IdConfiguracionPago")),
            Version = reader["Version"] == DBNull.Value ? 1 : reader.GetInt32(reader.GetOrdinal("Version")),
            NombreVersion = reader["NombreVersion"]?.ToString() ?? string.Empty,
            PrecioBasePorHectarea = reader.GetDecimal(reader.GetOrdinal("PrecioBasePorHectarea")),
            TopePorcentajeAjuste = reader.GetDecimal(reader.GetOrdinal("PorcentajeTopeAjuste")),
            FechaVigenciaDesde = reader.GetDateTime(reader.GetOrdinal("FechaVigenciaDesde")),
            FechaVigenciaHasta = reader["FechaVigenciaHasta"] == DBNull.Value ? null : reader.GetDateTime(reader.GetOrdinal("FechaVigenciaHasta")),
            Activa = string.Equals(estado, "Activa", StringComparison.OrdinalIgnoreCase),
            CreadoPor = reader["IdAdministrador"] == DBNull.Value ? 0 : reader.GetInt32(reader.GetOrdinal("IdAdministrador")),
            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
            Ajustes = new List<ConfiguracionPagoAjusteDTO>()
        };
    }
}
