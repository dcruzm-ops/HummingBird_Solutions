using Microsoft.Data.SqlClient;
using PSA.DataAccess;
using PSA.EntidadesDTO.DTOs.Evaluaciones;

namespace PSA.DataAccess.DAO;

public class EvaluacionEvidenciaDAO(IDbConnectionFactory connectionFactory)
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<int> CrearAsync(int idEvaluacion, string nombreArchivo, string rutaArchivo, string tipoArchivo, int cargadoPor)
    {
        const string sql = @"INSERT INTO dbo.EvaluacionEvidencias(IdEvaluacion, NombreArchivo, RutaArchivo, TipoArchivo, CargadoPor)
VALUES(@IdEvaluacion,@NombreArchivo,@RutaArchivo,@TipoArchivo,@CargadoPor);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
        command.Parameters.AddWithValue("@NombreArchivo", nombreArchivo);
        command.Parameters.AddWithValue("@RutaArchivo", rutaArchivo);
        command.Parameters.AddWithValue("@TipoArchivo", tipoArchivo);
        command.Parameters.AddWithValue("@CargadoPor", cargadoPor);
        await connection.OpenAsync();
        return Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
    }

    public async Task<List<EvaluacionEvidenciaDTO>> ObtenerPorEvaluacionAsync(int idEvaluacion)
    {
        const string sql = @"SELECT IdEvidenciaEvaluacion, IdEvaluacion, NombreArchivo, RutaArchivo, TipoArchivo, FechaCarga, CargadoPor
FROM dbo.EvaluacionEvidencias
WHERE IdEvaluacion=@IdEvaluacion
ORDER BY FechaCarga DESC;";
        var list = new List<EvaluacionEvidenciaDTO>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdEvaluacion", idEvaluacion);
        await connection.OpenAsync();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new EvaluacionEvidenciaDTO
            {
                IdEvidenciaEvaluacion = reader.GetInt32(0),
                IdEvaluacion = reader.GetInt32(1),
                NombreArchivo = reader.GetString(2),
                RutaArchivo = reader.GetString(3),
                TipoArchivo = reader.GetString(4),
                FechaCarga = reader.GetDateTime(5),
                CargadoPor = reader.GetInt32(6),
                UrlDescarga = reader.GetString(3)
            });
        }

        return list;
    }
}
